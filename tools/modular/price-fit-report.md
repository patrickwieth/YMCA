# Joint Battlemaster price study

Exploratory model, not an accepted balance change. Active catalog remains unchanged.
Base hardware anchored at 950 credits (0 CP). Targets read from catalog.json.
Price = manufacturing factor * complete hardware - CP count * global CP credit value.
Manufacturing has 1 CP, no separate hardware fee, and replaces the earlier cheaper-chassis assumption.
Only mass-production variants receive its common factor. Other variants use factor 1.
Three shared nonnegative hardware premiums are fitted jointly, not one adjustment per design.
PDL/Reflector package premiums include required motor/generator changes, not just the module.
Autoloader and nuclear ammunition share one price premium in this study, not their mechanics.

Objective: equal-weight squared credit residuals across 9 upgraded references.
Factor grid: 0.001 through 1.000; listed CP candidates only. No claim of a unique global optimum.
RMSE and maximum errors below are credits, not percentages. No gameplay strength fitting.

| Credits/CP | Production factor | Upgrade hardware premium | PDL package premium | Reflector package premium | RMSE | Max error |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0.628 | 0.3 | 648.5 | 0.7 | 2.1 | 3.9 |
| 100 | 0.724 | 98.2 | 742.6 | 112.9 | 15.4 | 30.5 |
| 200 | 0.812 | 199.1 | 831.9 | 220.9 | 27.8 | 49.2 |
| 300 | 0.893 | 302.6 | 917.2 | 324.9 | 39.9 | 67.5 |
| 400 | 0.969 | 409.3 | 998.2 | 424.0 | 52.4 | 87.8 |
| 600 | 1.000 | 550.0 | 1283.3 | 716.7 | 103.2 | 250.0 |
| 1000 | infeasible: nonpositive unit price | - | - | - | - | - |
| 1500 | infeasible: nonpositive unit price | - | - | - | - | - |
| 2000 | infeasible: nonpositive unit price | - | - | - | - | - |

## All reference price residuals

| Credits/CP | Existing vehicle | Legacy price | Fitted price | Difference |
|---:|---|---:|---:|---:|
| 0 | Battlemaster Autoloader | 950 | 950.3 | +0.3 (+0.0%) |
| 0 | Battlemaster Autoloader PDL | 1600 | 1598.8 | -1.2 (-0.1%) |
| 0 | Battlemaster Autoloader Reflector | 950 | 950.9 | +0.9 (+0.1%) |
| 0 | Battlemaster Nuclear Shells | 950 | 950.3 | +0.3 (+0.0%) |
| 0 | Battlemaster Nuclear Shells PDL | 1600 | 1598.8 | -1.2 (-0.1%) |
| 0 | Battlemaster Nuclear Shells Reflector | 950 | 950.9 | +0.9 (+0.1%) |
| 0 | Battlemaster Mass Production | 600 | 596.6 | -3.4 (-0.6%) |
| 0 | Battlemaster Mass Production PDL | 1000 | 1003.9 | +3.9 (+0.4%) |
| 0 | Battlemaster Mass Production Reflector | 600 | 597.0 | -3.0 (-0.5%) |
| 100 | Battlemaster Autoloader | 950 | 948.2 | -1.8 (-0.2%) |
| 100 | Battlemaster Autoloader PDL | 1600 | 1590.8 | -9.2 (-0.6%) |
| 100 | Battlemaster Autoloader Reflector | 950 | 961.0 | +11.0 (+1.2%) |
| 100 | Battlemaster Nuclear Shells | 950 | 948.2 | -1.8 (-0.2%) |
| 100 | Battlemaster Nuclear Shells PDL | 1600 | 1590.8 | -9.2 (-0.6%) |
| 100 | Battlemaster Nuclear Shells Reflector | 950 | 961.0 | +11.0 (+1.2%) |
| 100 | Battlemaster Mass Production | 600 | 587.8 | -12.2 (-2.0%) |
| 100 | Battlemaster Mass Production PDL | 1000 | 1025.5 | +25.5 (+2.5%) |
| 100 | Battlemaster Mass Production Reflector | 600 | 569.5 | -30.5 (-5.1%) |
| 200 | Battlemaster Autoloader | 950 | 949.1 | -0.9 (-0.1%) |
| 200 | Battlemaster Autoloader PDL | 1600 | 1581.0 | -19.0 (-1.2%) |
| 200 | Battlemaster Autoloader Reflector | 950 | 970.0 | +20.0 (+2.1%) |
| 200 | Battlemaster Nuclear Shells | 950 | 949.1 | -0.9 (-0.1%) |
| 200 | Battlemaster Nuclear Shells PDL | 1600 | 1581.0 | -19.0 (-1.2%) |
| 200 | Battlemaster Nuclear Shells Reflector | 950 | 970.0 | +20.0 (+2.1%) |
| 200 | Battlemaster Mass Production | 600 | 571.4 | -28.6 (-4.8%) |
| 200 | Battlemaster Mass Production PDL | 1000 | 1046.9 | +46.9 (+4.7%) |
| 200 | Battlemaster Mass Production Reflector | 600 | 550.8 | -49.2 (-8.2%) |
| 300 | Battlemaster Autoloader | 950 | 952.6 | +2.6 (+0.3%) |
| 300 | Battlemaster Autoloader PDL | 1600 | 1569.9 | -30.1 (-1.9%) |
| 300 | Battlemaster Autoloader Reflector | 950 | 977.5 | +27.5 (+2.9%) |
| 300 | Battlemaster Nuclear Shells | 950 | 952.6 | +2.6 (+0.3%) |
| 300 | Battlemaster Nuclear Shells PDL | 1600 | 1569.9 | -30.1 (-1.9%) |
| 300 | Battlemaster Nuclear Shells Reflector | 950 | 977.5 | +27.5 (+2.9%) |
| 300 | Battlemaster Mass Production | 600 | 548.4 | -51.6 (-8.6%) |
| 300 | Battlemaster Mass Production PDL | 1000 | 1067.5 | +67.5 (+6.7%) |
| 300 | Battlemaster Mass Production Reflector | 600 | 538.4 | -61.6 (-10.3%) |
| 400 | Battlemaster Autoloader | 950 | 959.3 | +9.3 (+1.0%) |
| 400 | Battlemaster Autoloader PDL | 1600 | 1557.5 | -42.5 (-2.7%) |
| 400 | Battlemaster Autoloader Reflector | 950 | 983.2 | +33.2 (+3.5%) |
| 400 | Battlemaster Nuclear Shells | 950 | 959.3 | +9.3 (+1.0%) |
| 400 | Battlemaster Nuclear Shells PDL | 1600 | 1557.5 | -42.5 (-2.7%) |
| 400 | Battlemaster Nuclear Shells Reflector | 950 | 983.2 | +33.2 (+3.5%) |
| 400 | Battlemaster Mass Production | 600 | 520.5 | -79.5 (-13.2%) |
| 400 | Battlemaster Mass Production PDL | 1000 | 1087.8 | +87.8 (+8.8%) |
| 400 | Battlemaster Mass Production Reflector | 600 | 531.4 | -68.6 (-11.4%) |
| 600 | Battlemaster Autoloader | 950 | 900.0 | -50.0 (-5.3%) |
| 600 | Battlemaster Autoloader PDL | 1600 | 1583.3 | -16.7 (-1.0%) |
| 600 | Battlemaster Autoloader Reflector | 950 | 1016.7 | +66.7 (+7.0%) |
| 600 | Battlemaster Nuclear Shells | 950 | 900.0 | -50.0 (-5.3%) |
| 600 | Battlemaster Nuclear Shells PDL | 1600 | 1583.3 | -16.7 (-1.0%) |
| 600 | Battlemaster Nuclear Shells Reflector | 950 | 1016.7 | +66.7 (+7.0%) |
| 600 | Battlemaster Mass Production | 600 | 350.0 | -250.0 (-41.7%) |
| 600 | Battlemaster Mass Production PDL | 1000 | 1033.3 | +33.3 (+3.3%) |
| 600 | Battlemaster Mass Production Reflector | 600 | 466.7 | -133.3 (-22.2%) |

Lowest error among sampled CP values: 0 credits/CP, RMSE 2.1.
This does NOT measure the value of a commander point. A small fitted value can expose model inadequacy.

## Why a perfect positive-CP fit is impossible under these assumptions

Legacy Reflector adds zero unit price in both regular and mass-production families.
An exact regular fit therefore requires Q = R (package premium = CP value).
An exact manufacturing fit also requires f*Q = R. For R > 0 these imply f = 1.
But f = 1 makes the identical PDL package increment equal in both families;
legacy increments are +650 and +400. Changing R alone cannot remove this contradiction.
Even R = 0 is not exact: base mass production needs f = 600/950,
whereas its PDL increment needs f = 400/650.

Thus a global proportional manufacturing discount is useful but does not exactly reproduce
all these legacy prices with identical defense packages. No special rebate is added.
Richer shared manufacturing/hardware rules, different actual packages, or accepted residuals
need an explicit design decision. Individual hardware allocations remain underdetermined.
CP values >= 950 are infeasible HERE because mass-production base hardware is fixed at 950
and no manufacturing hardware fee is allowed: f*950 - R cannot be positive.
This is a limitation of this particular model, not evidence against high CP values in general.
Performance, faction rules and the engine are unchanged; low package prices may not support
the current proposed motor/generator costs and require a separate hardware feasibility audit.
