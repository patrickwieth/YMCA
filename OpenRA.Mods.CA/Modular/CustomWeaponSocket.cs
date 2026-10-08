namespace OpenRA.Mods.CA.Modular
{
	// Editor geometry only. Concrete weapon compatibility still comes from the native binding.
	public enum CustomWeaponKind { Cannon, BattleTankCannon, TwinCannon, DualGatling, TripleIon, MissileLauncher, SonicEmitter, PrismEmitter }

	public sealed record CustomWeaponSocket(CustomWeaponKind Kind, int X, int Y, int Width, int Height)
	{
		public bool Contains(int x, int y) => x >= X && y >= Y && x - X < Width && y - Y < Height;
		public bool Fits(int x, int y, int width, int height) => x == X && y == Y && width == Width && height == Height;

		public static CustomWeaponKind ForCarrier(string carrier) => carrier switch
		{
			"medium-cannon-mount" => CustomWeaponKind.BattleTankCannon,
			"sonic-turret" => CustomWeaponKind.SonicEmitter,
			"prism-turret" => CustomWeaponKind.PrismEmitter,
			"dual-gatling-turret" => CustomWeaponKind.DualGatling,
			"designer-marv-mount" => CustomWeaponKind.TripleIon,
			"designer-hmlrs-mount" or "missile-turret" or "designer-rocket-mount" => CustomWeaponKind.MissileLauncher,
			"designer-mammoth-mount" or "designer-mk2-mount" => CustomWeaponKind.TwinCannon,
			_ => CustomWeaponKind.Cannon
		};

		public static string Label(CustomWeaponKind kind) => kind switch
		{
			CustomWeaponKind.BattleTankCannon => "105mm Smoothbore Cannon",
			CustomWeaponKind.TwinCannon => "Twin cannons",
			CustomWeaponKind.DualGatling => "Dual Gatling guns",
			CustomWeaponKind.TripleIon => "Triple Ion Cannon",
			CustomWeaponKind.MissileLauncher => "Missile launcher",
			CustomWeaponKind.SonicEmitter => "Sonic emitter",
			CustomWeaponKind.PrismEmitter => "Prism emitter",
			_ => "Cannon"
		};
	}
}
