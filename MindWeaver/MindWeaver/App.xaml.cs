namespace MindWeaver
{
    public partial class App : Application
    {
        private readonly AppShell _shell;

        /// <summary>
        /// <see cref="AppShell"/> is injected by the MAUI DI container
        /// (registered as Singleton in <c>MauiProgram.cs</c>).
        ///
        /// Accepting it here rather than writing <c>new AppShell()</c>
        /// ensures that every service AppShell (or its pages) depend on
        /// is also resolved through DI — including <see cref="Views.NoteNativePage"/>
        /// and its <see cref="ViewModels.NoteViewModel"/>.
        /// </summary>
        public App(AppShell shell)
        {
            InitializeComponent();
            _shell = shell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Using AppShell as the window root populates Shell.Current,
            // which MauiNavigationService relies on for GoToAsync calls.
            return new Window(_shell) { Title = "MindWeaver" };
        }
    }
}
