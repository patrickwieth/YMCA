WorldLoadedDropship = function()
	players=Player.GetPlayers(function(p) return p end)
	table.remove(players) -- remove 'everyone'
	table.remove(players,1) -- remove 'neutral'
	table.remove(players,1) -- remove 'creeps'
end

Transports={}

TickDropship = function()
	local dropships
	for _,p in pairs(players)
	do
		--handle dropships
		dropships = p.GetActorsByType("dropship")
		for _,c in pairs(dropships)
		do
			if c.AmmoCount("primary")<1 and Transports[tostring(c)]==nil
			then
				Transports[tostring(c)]='unloading'
				local start=c.Location
				local angle=1/255*2*math.pi
				local dist=5
				local vector=CVec.New(-math.floor(math.sin(angle)*dist),-math.floor(math.cos(angle)*dist))
				c.Paradrop(start+vector)
			elseif c.AmmoCount("primary")==1 and Transports[tostring(c)]=='unloading'
			then
				Transports[tostring(c)]=nil
				c.Paradrop(CPos.New(0,0)) --HACK: relocate (non-removable) landing zone to remote location
				c.Stop()
			--transports killed while unlading will remain in the list forever, but will not appear in GetActorsByType, so no problem there. this is better for performance then checking and removing.
			end
		end
	end
end
