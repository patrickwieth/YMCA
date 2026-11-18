-- Kirovs enter from the lower left and attack the island/fleet. No skirmish AI
-- or global unit changes are needed; all targeting uses real aircraft weapons.
local defenders = {}
local raiders = {}
local allies, soviets
local ticks = 0
local origin

local Cell = function(u, v)
	u = u + 1
	v = v + 17
	local y = math.floor(v / 2) - u
	return CPos.New(v - y, y)
end

local ProtectSlot
ProtectSlot = function(actor, kind, location)
	local slot = { actor = actor, kind = kind, location = location }
	table.insert(defenders, slot)
	local Register
	Register = function(a)
		slot.actor = a
		Trigger.OnKilled(a, function()
			Trigger.AfterDelay(DateTime.Seconds(kind == "liberty.statue" and 2 or 10), function()
				Register(Actor.Create(kind, true, { Owner = allies, Location = location }))
			end)
		end)
	end
	Register(actor)
end

local TargetFor = function(index)
	-- Every third airship heads directly for Liberty; others bomb the escorts
	-- and flak positions. Reacquire targets when a defender is destroyed.
	if index % 3 == 0 and not defenders[1].actor.IsDead then return defenders[1].actor end
	for n = 1, #defenders do
		local a = defenders[(index + n - 1) % #defenders + 1].actor
		if not a.IsDead and a.IsInWorld then return a end
	end
	return nil
end

local wave = 0
local SpawnWave
SpawnWave = function()
	local live = {}
	Utils.Do(raiders, function(a)
		if not a.IsDead and a.IsInWorld then table.insert(live, a) end
	end)
	raiders = live
	wave = wave + 1
	if #raiders < 15 then
		for i = 0, 2 do
			local index = wave * 3 + i
			local unit = Actor.Create("liberty.kirov", true, { Owner = soviets, Location = Cell(29 + i * 2, 55 + (i % 2) * 2) })
			table.insert(raiders, unit)
			local Attack = function()
				local target = TargetFor(index)
				if target ~= nil then unit.Attack(target) end
			end
			-- Faction-granted bomb armaments settle after creation, not during it.
			Trigger.AfterDelay(2, Attack)
			Trigger.OnIdle(unit, Attack)
			local Retarget
			Retarget = function()
				if unit.IsDead or not unit.IsInWorld then return end
				unit.Stop()
				Attack()
				Trigger.AfterDelay(DateTime.Seconds(8), Retarget)
			end
			Trigger.AfterDelay(DateTime.Seconds(8), Retarget)
			Trigger.AfterDelay(DateTime.Seconds(90), function()
				if not unit.IsDead and unit.IsInWorld then unit.Destroy() end
			end)
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(12), SpawnWave)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Raiders")
	origin = CameraAnchor.CenterPosition
	Camera.Position = origin
	ProtectSlot(Liberty, "liberty.statue", Liberty.Location)
	Utils.Do({ Flak0, Flak1, Flak2, Flak3, Flak4, Flak5 }, function(a) ProtectSlot(a, "agun", a.Location) end)
	Utils.Do({ Cruiser0, Cruiser1, Cruiser2, Cruiser3, Cruiser4, Cruiser5, Cruiser6, Cruiser7, Cruiser8, Cruiser9 }, function(a)
		ProtectSlot(a, "aegis_cruiser", a.Location)
	end)
	SpawnWave()
end

Tick = function()
	ticks = ticks + 1
	-- Keep the landmark in the upper-right central area, not behind the portrait.
	if origin ~= nil then Camera.Position = origin end
end
