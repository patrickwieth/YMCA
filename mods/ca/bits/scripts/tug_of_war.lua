WorldLoadedToW = function()
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

	players = { mp0, mp1, mp2, mp3, mp4, mp5, mp6, mp7, mp8, mp9, mp10, mp11 }
end

autoproduced = {}
teamAunits = {{}, {}, {}, {}, {}, {}, {}}
teamBunits = {{}, {}, {}, {}, {}, {}, {}}

warpoint0 = Map.NamedActor("warpoint0")
warpointa1 = Map.NamedActor("warpointa1")
warpointa2 = Map.NamedActor("warpointa2")
warpointa3 = Map.NamedActor("warpointa3")
warpointb1 = Map.NamedActor("warpointb1")
warpointb2 = Map.NamedActor("warpointb2")
warpointb3 = Map.NamedActor("warpointb3")

if (warpointa3 ~= nil) then
	insertIndex = 1
	if (warpointb3  == nil) then
		Media.DisplayMessage("warpointa3 exists but warpointb3 does not, this is not a good idea...")
	end
	if (warpointa2 == nil or warpointb2 == nil or warpointa1 == nil or warpointb1 == nil or warpoint0 == nil) then
		Media.DisplayMessage("if you use warpointa3 you also need all other warpoints a1,a2,b1,b2 and 0.")
	end
elseif ( warpointa2 ~= nil ) then
	insertIndex = 3
	if (warpointb2  == nil) then
		Media.DisplayMessage("warpointa2 exists but warpointb2 does not, this is not a good idea...")
	end
	if (warpointa1 == nil or warpointb1 == nil or warpoint0 == nil) then
		Media.DisplayMessage("if you use warpointa2 you also need the lower warpoints a1,b1 and 0.")
	end
else
	if (warpointa1 == nil or warpointb1 == nil or warpoint0 == nil) then
		Media.DisplayMessage("you need at least the warpoints a1,b1 and 0.")
	end
	insertIndex = 5
end


TickTugOfWar = function()
  if DateTime.GameTime % 50 > 0
  then
    return
  end
	--Media.DisplayMessage(tostring(DateTime.GameTime))

	if DateTime.GameTime % 500 == 0
	then
		--Media.DisplayMessage(tostring(#autoproduced))
		TransferUnitGroups()
		AttackMoveUnits()
	end

end


AttackMoveUnitsFromTable = function (unittable, Location)
	for i,unit in next,unittable do
  	--Media.DisplayMessage(tostring(key) .. " of " .. tostring(#autoproduced) .. " is " .. tostring(value.Type))
	end
	for i=1,#unittable do
		--Media.DisplayMessage(tostring(i) .. " of " .. tostring(#unittable) .. " is " .. tostring(unittable[i].Type))
	end

	for i=1,#unittable do
		--Media.DisplayMessage(tostring(i) .. " of " .. tostring(#autoproduced))
		if unittable[i].IsDead  then --and unittable[i] ~= nil
			--table.remove(unittable, i)			-- crash area
		else
			unittable[i].AttackMove(Location, 0)
		end
	end
end

AttackMoveUnits = function()
	if ( warpointa3 ~= nil ) then
		AttackMoveUnitsFromTable(teamAunits[1], warpointa3.Location)
		AttackMoveUnitsFromTable(teamAunits[2], warpointa2.Location)
		AttackMoveUnitsFromTable(teamAunits[3], warpointa1.Location)
		AttackMoveUnitsFromTable(teamAunits[4], warpoint0.Location)
		AttackMoveUnitsFromTable(teamAunits[5], warpointb1.Location)
		AttackMoveUnitsFromTable(teamAunits[6], warpointb2.Location)
		AttackMoveUnitsFromTable(teamAunits[7], warpointb3.Location)

	elseif ( warpointa2 ~= nil ) then
		AttackMoveUnitsFromTable(teamAunits[3], warpointa2.Location)
		AttackMoveUnitsFromTable(teamAunits[4], warpointa1.Location)
		AttackMoveUnitsFromTable(teamAunits[5], warpoint0.Location)
		AttackMoveUnitsFromTable(teamAunits[6], warpointb1.Location)
		AttackMoveUnitsFromTable(teamAunits[7], warpointb2.Location)

	else
		Media.DisplayMessage("short move")
		AttackMoveUnitsFromTable(teamAunits[5], warpointa1.Location)
		AttackMoveUnitsFromTable(teamAunits[6], warpoint0.Location)
		AttackMoveUnitsFromTable(teamAunits[7], warpointb1.Location)
	end

end


TransferUnitGroups = function()
	-- team A units first go into higher lists
	for i, unit in pairs(teamAunits[6]) do
		table.insert(teamAunits[7], unit)
	end
	teamAunits[6] = teamAunits[5]
	teamAunits[5] = teamAunits[4]
	teamAunits[4] = teamAunits[3]
	teamAunits[3] = teamAunits[2]
	teamAunits[2] = teamAunits[1]
	teamAunits[1] = {}

	-- team B units go into higher lists afterwards
	for i, unit in pairs(teamBunits[6]) do
		table.insert(teamBunits[7], unit)
	end
	teamBunits[6] = teamBunits[5]
	teamBunits[5] = teamBunits[4]
	teamBunits[4] = teamBunits[3]
	teamBunits[3] = teamBunits[2]
	teamBunits[2] = teamBunits[1]
	teamBunits[1] = {}

	Media.DisplayMessage("length of 7 team A before split " .. tostring( #teamAunits[7] ))
	Media.DisplayMessage("length of 7 team B before split " .. tostring( #teamBunits[7] ))
	-- sort units into specific arrays

	--for i, unit in pairs(autoproduced) do
	for i=1,#autoproduced do


		--Media.DisplayMessage("sorting unit #" .. tostring( i ))
		-- split new units up depending on team
		if (autoproduced[i].Owner.Team == 0)
		then
			--Media.DisplayMessage(tostring(i) .. " in team " .. tostring( autoproduced[i].Owner.Team ))
			table.insert(teamAunits[insertIndex], autoproduced[i])
			--table.remove(autoproduced, i)

		else
			--Media.DisplayMessage(tostring(i) .. " in yes" .. tostring( autoproduced[i].Owner.Team ))
			table.insert(teamBunits[insertIndex], autoproduced[i])
			--table.remove(autoproduced, i)

		end

	end
	autoproduced = {}

end


Trigger.OnAnyProduction(
  function(producer, produced, productionType)

    if producer.Type == 'weap.tow'
    then
			--Media.DisplayMessage("insert at # " .. #autoproduced+1 .. "autoproduced length " .. tostring( #autoproduced ))

      --table.insert(autoproduced, produced)
			autoproduced[#autoproduced+1] = produced

    end
	end
)
