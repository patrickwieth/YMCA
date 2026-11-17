-- Individual attack-moving bombers enter from beyond the southwest viewport.
-- Ships fill vacancies from the rear; replacements sail in from the northeast.
local allies, soviets, origin
local raiders = {}
local fleet = {}
local ticks = 0
local statueStanding = true
local Cell = function(u, v)
	u = u + 29
	v = v + 45
	local y = math.floor(v / 2) - u
	return CPos.New(v - y, y)
end
local Alive = function(a) return a ~= nil and not a.IsDead and a.IsInWorld end

local MaintainBuilding = function(actor, kind, location)
	local Register
	Register = function(a)
		Trigger.OnKilled(a, function()
			Trigger.AfterDelay(DateTime.Seconds(15), function()
				if statueStanding then
					Register(Actor.Create(kind, true, { Owner = allies, Location = location }))
				end
			end)
		end)
	end
	Register(actor)
end

local FillVacancy
local RegisterShip = function(ship)
	Trigger.OnKilled(ship, function()
		for i = 1, #fleet do
			if fleet[i].actor == ship then
				fleet[i].actor = nil
				Trigger.AfterDelay(1, function() FillVacancy(i) end)
				return
			end
		end
	end)
end
FillVacancy = function(index)
	if Alive(fleet[index].actor) then return end
	-- Move the next surviving rear ship into the gap, propagating the vacancy.
	for j = index + 1, #fleet do
		if Alive(fleet[j].actor) then
			local ship = fleet[j].actor
			fleet[index].actor = ship
			fleet[j].actor = nil
			ship.Stop()
			ship.Move(fleet[index].location)
			FillVacancy(j)
			return
		end
	end
	if not statueStanding or fleet[index].pending then return end
	fleet[index].pending = true
	Trigger.AfterDelay(DateTime.Seconds(18), function()
		fleet[index].pending = false
		if not statueStanding or Alive(fleet[index].actor) then return end
		local ship = Actor.Create("aegis_cruiser", true, { Owner = allies, Location = Cell(54, -6) })
		fleet[index].actor = ship
		RegisterShip(ship)
		Trigger.AfterDelay(2, function() ship.Move(fleet[index].location) end)
	end)
end

local SendBomber = function(location)
	local unit = Actor.Create("liberty.kirov", true, { Owner = soviets, Location = location })
	table.insert(raiders, unit)
	local Advance = function()
		if not Alive(unit) then return end
		unit.Stance = "AttackAnything"
		unit.AttackMove(Cell(38, 33))
	end
	Trigger.AfterDelay(2, Advance)
	Trigger.OnIdle(unit, Advance)
	Trigger.AfterDelay(DateTime.Seconds(240), function()
		if Alive(unit) then unit.Destroy() end
	end)
end
local SpawnNext
SpawnNext = function()
	if not statueStanding then return end
	local live = {}
	Utils.Do(raiders, function(a) if Alive(a) then table.insert(live, a) end end)
	raiders = live
	if #raiders < 48 then SendBomber(Cell(-25, 96)) end
	-- Fourteen seconds between recurring Kirovs; the opening column is unchanged.
	Trigger.AfterDelay(DateTime.Seconds(14), SpawnNext)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Raiders")
	origin = CameraAnchor.CenterPosition
	Camera.Position = origin
	Trigger.OnKilled(Liberty, function() statueStanding = false end)
	Utils.Do({ Flak0, Flak1, Flak2, Flak3, Flak4, Flak5 }, function(a) MaintainBuilding(a, "agun", a.Location) end)
	Utils.Do({ Cruiser0, Cruiser1, Cruiser2, Cruiser3, Cruiser4, Cruiser5, Cruiser6, Cruiser7, Cruiser8, Cruiser9 }, function(a)
		table.insert(fleet, { actor = a, location = a.Location, pending = false })
		RegisterShip(a)
	end)
	-- This column exists before the first rendered frame; ongoing spawns are all
	-- far offscreen. The scene starts already in motion instead of a long wait.
	for i = 1, 18 do
		local t = i / 18
		SendBomber(Cell(math.floor(-25 + 59 * t), math.floor(96 - 48 * t)))
	end
	SpawnNext()
end
Tick = function()
	ticks = ticks + 1
	if origin ~= nil then Camera.Position = origin end
end
