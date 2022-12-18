--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

StartingPoints = Map.LobbyOption("commanderpoints")

Media.Debug(StartingPoints)

if StartingPoints == "noinit" then
	PointsPerRank = { 0, 1, 1, 1, 2 }
elseif StartingPoints == "fewer" then
	PointsPerRank = { 3, 1, 1, 1, 1 }
elseif StartingPoints == "normal" then
	PointsPerRank = { 6, 1, 1, 1, 2 }
elseif StartingPoints == "later" then
	PointsPerRank = { 3, 2, 2, 2, 3 }
elseif StartingPoints == "toomany" then
	PointsPerRank = { 12, 1, 1, 1, 2 }
elseif StartingPoints == "all" then
	PointsPerRank = { 100, 1, 1, 1, 2 }
end

PointActorExists =
{
	Multi0 = false,
	Multi1 = false,
	Multi2 = false,
	Multi3 = false,
	Multi4 = false,
	Multi5 = false,
	Multi6 = false,
	Multi7 = false,
	Multi8 = false,
	Multi9 = false,
	Multi10 = false,
	Multi11 = false,
  Multi12 = false,
  Multi13 = false,
  Multi14 = false,
  Multi15 = false,
}

Points =
{
	Multi0 = PointsPerRank[1],
	Multi1 = PointsPerRank[1],
	Multi2 = PointsPerRank[1],
	Multi3 = PointsPerRank[1],
	Multi4 = PointsPerRank[1],
	Multi5 = PointsPerRank[1],
	Multi6 = PointsPerRank[1],
	Multi7 = PointsPerRank[1],
	Multi8 = PointsPerRank[1],
	Multi9 = PointsPerRank[1],
	Multi10 = PointsPerRank[1],
	Multi11 = PointsPerRank[1],
  Multi12 = PointsPerRank[1],
  Multi13 = PointsPerRank[1],
  Multi14 = PointsPerRank[1],
  Multi15 = PointsPerRank[1]
}

HasPointsActors =
{
	Multi0 = nil,
	Multi1 = nil,
	Multi2 = nil,
	Multi3 = nil,
	Multi4 = nil,
	Multi5 = nil,
	Multi6 = nil,
	Multi7 = nil,
	Multi8 = nil,
	Multi9 = nil,
	Multi10 = nil,
	Multi11 = nil,
  Multi12 = nil,
  Multi13 = nil,
  Multi14 = nil,
  Multi15 = nil
}

Levels =
{
	Multi0 = 0,
	Multi1 = 0,
	Multi2 = 0,
	Multi3 = 0,
	Multi4 = 0,
	Multi5 = 0,
	Multi6 = 0,
	Multi7 = 0,
	Multi8 = 0,
	Multi9 = 0,
	Multi10 = 0,
	Multi11 = 0,
  Multi12 = 0,
  Multi13 = 0,
  Multi14 = 0,
  Multi15 = 0
}

Ranks = { "1 Star General", "2 Stars General", "3 Stars General", "4 Stars General", "5 Stars General" }
RankXPs = { 0, 100, 300, 1000, 3000 }

ReducePoints = function(player)
	Trigger.OnProduction(player.GetActorsByType("player")[1], function()
		if Points[player.InternalName] > 0 then
			Points[player.InternalName] = Points[player.InternalName] - 1
		end
	end)
end

TickGeneralsPowers = function()
	for _,player in pairs(players) do
		if player.IsLocalPlayer then
			if Levels[player.InternalName] < 4 then
				UserInterface.SetMissionText("Current Rank: " .. Ranks[Levels[player.InternalName] + 1] .. "\nCommander Specialization Points: " .. Points[player.InternalName] .. "\nProgress to Next Rank: " .. player.Experience - RankXPs[Levels[player.InternalName] + 1] .. "/" .. RankXPs[Levels[player.InternalName] + 2] - RankXPs[Levels[player.InternalName] + 1] .. "", HSLColor.White)
			else
				UserInterface.SetMissionText("Current Rank: " .. Ranks[Levels[player.InternalName] + 1] .. "\nCommander Specialization Points: " .. Points[player.InternalName] .. "", HSLColor.White)
			end
		end

		if Points[player.InternalName] > 0 and not PointActorExists[player.InternalName] then
			HasPointsActors[player.InternalName] = Actor.Create("hack.has_points", true, { Owner = player })

			PointActorExists[player.InternalName] = true
		end

		if not (Points[player.InternalName] > 0) and PointActorExists[player.InternalName] and HasPointsActors[player.InternalName] ~= nil then
			HasPointsActors[player.InternalName].Destroy()

			PointActorExists[player.InternalName] = false
		end

		if player.Experience >= RankXPs[2] and not (Levels[player.InternalName] > 0) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[2]

			--[[ Media.PlaySpeechNotification(player, "RankUp") ]]
		end

		if player.Experience >= RankXPs[3] and not (Levels[player.InternalName] > 1) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[3]

			--[[ Media.PlaySpeechNotification(player, "RankUp")  ]]
			Actor.Create("hack.rank_3", true, { Owner = player })
		end

		if player.Experience >= RankXPs[4] and not (Levels[player.InternalName] > 2) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[4]

			--[[ Media.PlaySpeechNotification(player, "RankUp")  ]]
		end

		if player.Experience >= RankXPs[5] and not (Levels[player.InternalName] > 3) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[5]

			--[[ Media.PlaySpeechNotification(player, "RankUp")  ]]
			Actor.Create("hack.rank_5", true, { Owner = player })
		end
	end
end

WorldLoadedCommanderTree = function()
	neutral = Player.GetPlayer("Neutral")
	mp0 = Player.GetPlayer("Multi0")
	mp1 = Player.GetPlayer("Multi1")
	mp2 = Player.GetPlayer("Multi2")
	mp3 = Player.GetPlayer("Multi3")
	mp4 = Player.GetPlayer("Multi4")
	mp5 = Player.GetPlayer("Multi5")
	mp6 = Player.GetPlayer("Multi6")
	mp7 = Player.GetPlayer("Multi7")
	mp8 = Player.GetPlayer("Multi8")
	mp9 = Player.GetPlayer("Multi9")
	mp10 = Player.GetPlayer("Multi10")
	mp11 = Player.GetPlayer("Multi11")
  mp12 = Player.GetPlayer("Multi12")
  mp13 = Player.GetPlayer("Multi13")
  mp14 = Player.GetPlayer("Multi14")
  mp15 = Player.GetPlayer("Multi15")

	players = { mp0, mp1, mp2, mp3, mp4, mp5, mp6, mp7, mp8, mp9, mp10, mp11, mp12, mp13, mp14, mp15 }

	for _,player in pairs(players) do
		ReducePoints(player)
	end
end
