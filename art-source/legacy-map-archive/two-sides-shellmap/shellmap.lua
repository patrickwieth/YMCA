-- Two genuine normal AIs build, repair and produce; this script only keeps the
-- menu battle funded/endless and introduces one opening skirmish.
local ticks = 0
local origin
local armies = {}
local combatTypes = {
	mercenary = true, rocket_mercenary = true, grenadier = true,
	hmmv = true, mtnk = true, mammoth = true, mlrs = true,
	conscript = true, rpg_soldier = true, flamethrower = true,
	heavy_tank = true, btr = true, v2rl = true
}

local RetireEventually = function(unit)
	if not combatTypes[unit.Type] then return end
	-- Stalled units cannot accumulate forever while the main menu is unattended.
	Trigger.AfterDelay(DateTime.Seconds(180), function()
		if not unit.IsDead and unit.IsInWorld then unit.Destroy() end
	end)
end

WorldLoaded = function()
	local gdi = Player.GetPlayer("GDI")
	local soviets = Player.GetPlayer("Soviets")
	armies = { gdi, soviets }
	origin = Map.CenterOfCell(CPos.New(82, -17))
	Camera.Position = origin

	-- Defeat cannot permanently end a shellmap. Only the two core yards and
	-- their single home guards are protected; every other structure is rebuildable.
	Utils.Do({ GDIConyard, SovietsConyard, GDIGuard, SovietsGuard }, function(a)
		a.GrantCondition("shellmap-core")
	end)

	Trigger.OnAnyProduction(function(_, unit, _)
		if unit.Owner == gdi or unit.Owner == soviets then RetireEventually(unit) end
	end)

	local opening = {
		{ gdi, { "mtnk", "hmmv", "mercenary", "rocket_mercenary" }, CPos.New(82, -28), SovietStart.Location },
		{ soviets, { "heavy_tank", "btr", "conscript", "rpg_soldier" }, CPos.New(82, -6), GDIStart.Location }
	}
	Utils.Do(opening, function(wave)
		local units = Reinforcements.Reinforce(wave[1], wave[2], { wave[3] }, 8)
		Utils.Do(units, function(unit)
			unit.AttackMove(wave[4])
			RetireEventually(unit)
		end)
	end)
end

Tick = function()
	ticks = ticks + 1
	if ticks % DateTime.Seconds(10) == 0 then
		Utils.Do(armies, function(player)
			if player.Cash < 20000 then player.Cash = 20000 end
		end)
	end

	-- Slow drift across the crossing: no abrupt jumps or distracting camera cuts.
	if origin ~= nil then
		local t = ticks / 1400
		Camera.Position = origin + WVec.New(math.floor(2200 * math.sin(t)), math.floor(900 * math.sin(t * 0.7)), 0)
	end
end
