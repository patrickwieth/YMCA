using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
    public class CheckpointOriginInfo : TraitInfo
    {
        public override object Create(ActorInitializer init) { return new CheckpointOrigin(init, this); }
    }

    public class CheckpointOrigin
    {
      public bool HierarchyAscending;
      public Actor HomeCheckpoint;
      readonly CheckpointOriginInfo info;

      public CheckpointOrigin(ActorInitializer init, CheckpointOriginInfo info)
      {
          this.info = info;
      }

      public void initCheckpointOrigin (Actor newHomeCP) {
        HomeCheckpoint = newHomeCP;

        //TextNotificationsManager.Debug("HomeCheckpoint"+HomeCheckpoint);

        if (HomeCheckpoint != null)
        {
          if (HomeCheckpoint.TraitOrDefault<Checkpoint>().Hierarchy < 0)
            HierarchyAscending = true;
          else
            HierarchyAscending = false;
        }
        //TextNotificationsManager.Debug("ascending set "+HierarchyAscending);
      }
    }
}
