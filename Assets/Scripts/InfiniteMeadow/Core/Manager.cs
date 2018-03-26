namespace InfiniteMeadow.Core {
	internal abstract class Manager<T>
	{
		protected static T Instance { get; set; }
		public static T GetInstance() { return Instance; }
	}
}