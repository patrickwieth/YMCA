
TickTugOfWar = function()
  if DateTime.GameTime % 25 > 0
  then
    return
  end

  Media.Debug("bla" )
  warpoint0 = Map.NamedActor("WarPoint0")

  if ( Map.NamedActor("WarPoint0") ~= nil )
  then
    Media.DisplayMessage(tostring(DateTime.GameTime))

    for _,player in pairs(players) do
      for _,unit in pairs(player.GetGroundAttackers()) do
        unit.AttackMove(warpoint0.Location, 0)
      end
    end
  else
    --Media.DisplayMessage("warpoint0 not found")
  end


end
