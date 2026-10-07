namespace OpenRA.Mods.CA.Modular
{
	public enum CustomVehicleSilhouetteKind { Tank, LightVehicle, Wheeled, Bike, Walker, Tripod, Hover }

	public static class CustomVehicleSilhouette
	{
		// Presentation only. Use explicit native actor bindings, not translated names,
		// component mass or shared running-gear labels (which can also serve wheeled units).
		public static CustomVehicleSilhouetteKind ForActor(string actor, bool nativeHover = false)
		{
			return actor?.ToLowerInvariant() switch
			{
				"hmmv" or "bggy" or "jeep" => CustomVehicleSilhouetteKind.LightVehicle,
				"bike" => CustomVehicleSilhouetteKind.Bike,
				"titn" or "juggernaut" or "mammothmk2" or "xo" or "gunw" => CustomVehicleSilhouetteKind.Walker,
				"tpod" or "hexapod" => CustomVehicleSilhouetteKind.Tripod,
				"btr" or "apc2" or "ifv" or "katy" or "v3rl" or "hq7" or "htk5" => CustomVehicleSilhouetteKind.Wheeled,
				_ => nativeHover ? CustomVehicleSilhouetteKind.Hover : CustomVehicleSilhouetteKind.Tank
			};
		}
	}
}
