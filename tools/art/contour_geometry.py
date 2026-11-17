"""Native, rotation-equivariant ownership of visible surface-boundary pixels."""
import numpy as np

def owned_boundaries(label, protected):
    """One stable surface owns each boundary, never screen-left or screen-top.

    Label zero is transparent. Protected pixels keep narrow colored details.
    No dilation, resampling, texture-edge detection or silhouette growth.
    """
    edge = np.zeros_like(label, dtype=bool)
    for dy, dx in ((0, 1), (1, 0)):
        other = np.roll(label, (-dy, -dx), (0, 1))
        other_protected = np.roll(protected, (-dy, -dx), (0, 1))
        boundary = (label > 0) & (other > 0) & (label != other)
        choose_here = ((label > other) | other_protected) & ~protected
        edge |= boundary & choose_here
        edge |= np.roll(boundary & ~choose_here & ~other_protected, (dy, dx), (0, 1))
    return edge
