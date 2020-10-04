WorldLoaded = function()
  WorldLoadedCommanderTree()
  WorldLoadedDropship()
  if (Player.GetPlayer("Multi0").HasPrerequisites({"tugofwar"}) == true)
  then
    Media.DisplayMessage("deine mudder, schwänze in der hölle... du weißt schon")
    WorldLoadedToW()
  end
end

Tick = function()
	TickGeneralsPowers()
  TickDropship()
  if Player.GetPlayer("Multi0").HasPrerequisites({"tugofwar"})
  then TickTugOfWar()
  end
end
