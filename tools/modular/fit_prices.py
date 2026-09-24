"""Exploratory Battlemaster price fit; does not change catalog or engine rules."""
import argparse
import json
import math
from pathlib import Path

CP_CANDIDATES = (0, 100, 200, 300, 400, 600, 1000, 1500, 2000)


def family_targets(catalog):
    names = {d['name']: d for d in catalog['designs']}
    base = names['Battlemaster baseline']['target']['cost']
    families = ('Autoloader', 'Nuclear Shells', 'Mass Production')
    return base, [(f'Battlemaster {family}{suffix}',
                   names[f'Battlemaster {family}{suffix}']['target']['cost'])
                  for family in families for suffix in ('', ' PDL', ' Reflector')]


def rows_for(base, targets, cp, factor):
    # Unknowns: shared first upgrade premium A, complete PDL package P,
    # complete Reflector package Q. A is shared by autoloader and nuclear
    # ammunition here because their legacy price/CP targets are identical.
    rows = []
    for index, (_, target) in enumerate(targets):
        family, defense = divmod(index, 3)
        manufacturing = family == 2
        f = factor if manufacturing else 1.0
        coefficients = [0.0 if manufacturing else 1.0,
                        f if defense == 1 else 0.0,
                        f if defense == 2 else 0.0]
        cp_count = 1 if defense == 0 else 2
        constant = f * base - cp_count * cp
        rows.append((coefficients, target - constant, constant))
    return rows


def nonnegative_fit(rows):
    # Convex least squares, cyclic exact coordinate minimization, only 3 unknowns.
    gram = [[sum(a[i] * a[j] for a, _, _ in rows) for j in range(3)] for i in range(3)]
    rhs = [sum(a[i] * b for a, b, _ in rows) for i in range(3)]
    x = [0.0] * 3
    for _ in range(500):
        before = x.copy()
        for i in range(3):
            x[i] = max(0.0, (rhs[i] - sum(gram[i][j] * x[j] for j in range(3) if j != i)) / gram[i][i])
        if max(abs(x[i] - before[i]) for i in range(3)) < 1e-8:
            return x
    raise RuntimeError('Price fit did not converge')


def fit_at(base, targets, cp, factor):
    if not math.isfinite(cp) or cp < 0 or not math.isfinite(factor) or not 0 < factor <= 1:
        raise ValueError('Need nonnegative finite CP value and manufacturing factor in (0, 1]')
    rows = rows_for(base, targets, cp, factor)
    values = nonnegative_fit(rows)
    predictions = [constant + sum(a[i] * values[i] for i in range(3)) for a, _, constant in rows]
    if min(predictions) <= 0:
        return None
    residuals = [predicted - target for predicted, (_, target) in zip(predictions, targets)]
    return dict(cp=cp, factor=factor, upgrade=values[0], pdl=values[1], reflector=values[2],
                predictions=predictions, residuals=residuals,
                rmse=math.sqrt(sum(r * r for r in residuals) / len(residuals)),
                max_error=max(abs(r) for r in residuals))


def scan(base, targets, cp, steps=1000):
    if steps < 1:
        raise ValueError('Factor grid must have at least one step')
    candidates = (fit_at(base, targets, cp, step / steps) for step in range(1, steps + 1))
    return min((r for r in candidates if r is not None), key=lambda r: r['rmse'], default=None)


def report(catalog):
    base, targets = family_targets(catalog)
    results = [scan(base, targets, cp) for cp in CP_CANDIDATES]
    feasible = [r for r in results if r is not None]
    lines = ['# Joint Battlemaster price study', '',
             'Exploratory model, not an accepted balance change. Active catalog remains unchanged.',
             f"Reference source blocks audited at {catalog['reference_commit']}; see {catalog['reference_audit']}.",
             f'Base hardware anchored at {base:g} credits (0 CP). Targets read from catalog.json.',
             'Price = manufacturing factor * complete hardware - CP count * global CP credit value.',
             'Manufacturing has 1 CP, no separate hardware fee, and replaces the earlier cheaper-chassis assumption.',
             'Only mass-production variants receive its common factor. Other variants use factor 1.',
             'Three shared nonnegative hardware premiums are fitted jointly, not one adjustment per design.',
             'PDL/Reflector package premiums include required motor/generator changes, not just the module.',
             'Autoloader and nuclear ammunition share one price premium in this study, not their mechanics.', '',
             'Objective: equal-weight squared credit residuals across 9 upgraded references.',
             'Factor grid: 0.001 through 1.000; listed CP candidates only. No claim of a unique global optimum.',
             'RMSE and maximum errors below are credits, not percentages. No gameplay strength fitting.', '',
             '| Credits/CP | Production factor | Upgrade hardware premium | PDL package premium | Reflector package premium | RMSE | Max error |',
             '|---:|---:|---:|---:|---:|---:|---:|']
    for cp, r in zip(CP_CANDIDATES, results):
        if r is None:
            lines.append(f"| {cp:g} | infeasible: nonpositive unit price | - | - | - | - | - |")
        else:
            lines.append(f"| {r['cp']:g} | {r['factor']:.3f} | {r['upgrade']:.1f} | {r['pdl']:.1f} | {r['reflector']:.1f} | {r['rmse']:.1f} | {r['max_error']:.1f} |")
    # Show all reference residuals, not only the cheapest/best candidate.
    lines += ['', '## All reference price residuals', '',
              '| Credits/CP | Existing vehicle | Legacy price | Fitted price | Difference |',
              '|---:|---|---:|---:|---:|']
    for r in feasible:
        for (name, target), predicted, residual in zip(targets, r['predictions'], r['residuals']):
            lines.append(f"| {r['cp']:g} | {name} | {target:g} | {predicted:.1f} | {residual:+.1f} ({residual / target * 100:+.1f}%) |")
    best = min(feasible, key=lambda r: r['rmse'])
    lines += ['', f"Lowest error among sampled CP values: {best['cp']:g} credits/CP, RMSE {best['rmse']:.1f}.",
              'This does NOT measure the value of a commander point. A small fitted value can expose model inadequacy.', '',
              '## Why a perfect positive-CP fit is impossible under these assumptions', '',
              'Legacy Reflector adds zero unit price in both regular and mass-production families.',
              'An exact regular fit therefore requires Q = R (package premium = CP value).',
              'An exact manufacturing fit also requires f*Q = R. For R > 0 these imply f = 1.',
              'But f = 1 makes the identical PDL package increment equal in both families;',
              'legacy increments are +650 and +400. Changing R alone cannot remove this contradiction.',
              'Even R = 0 is not exact: base mass production needs f = 600/950,',
              'whereas its PDL increment needs f = 400/650.', '',
              'Thus a global proportional manufacturing discount is useful but does not exactly reproduce',
              'all these legacy prices with identical defense packages. No special rebate is added.',
              'Richer shared manufacturing/hardware rules, different actual packages, or accepted residuals',
              'need an explicit design decision. Individual hardware allocations remain underdetermined.',
              'CP values >= 950 are infeasible HERE because mass-production base hardware is fixed at 950',
              'and no manufacturing hardware fee is allowed: f*950 - R cannot be positive.',
              'This is a limitation of this particular model, not evidence against high CP values in general.',
              'Performance, faction rules and the engine are unchanged; low package prices may not support',
              'the current proposed motor/generator costs and require a separate hardware feasibility audit.']
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    catalog = json.loads(Path(__file__).with_name('catalog.json').read_text(encoding='utf-8'))
    text = report(catalog)
    if args.output:
        args.output.write_text(text, encoding='utf-8')
    else:
        print(text, end='')
