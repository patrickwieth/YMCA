"""Normalize only presentation/anchor follow-ups covered by dedicated engine/grid checks."""
import re

def normalize_building_presentation(fields):
 for key in list(fields):
  names=[]
  if key=='RenderSprites':names=['SuppressBuildingAnimations','AnimatedBuildingSequences']
  elif key.startswith(('WithSpriteBody','WithChargeSpriteBody','WithResourceLevelSpriteBody')):names=['UseIdleFrameForRendering']
  elif key=='ActorPreviewPlaceBuildingPreview':names=['Animated']
  elif key=='IsometricSelectable':names=['DecorationOffset']
  elif key=='Building':names=['LocalCenterOffset'] # Exact world anchor and mine cells checked by test_bib_grid_alignment.py.
  for name in names:fields[key]=re.sub(r'\n\t'+name+r': [^\n]+','',fields[key])
 return fields
