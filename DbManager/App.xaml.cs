using System.IO;
using System.Windows;

namespace DbManager
{
	public partial class App : Application
	{
		private readonly TextWriter _errorWriter;

		public App()
		{
			_errorWriter = Console.Error;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			DispatcherUnhandledException += App_DispatcherUnhandledException;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			switch (e.ExceptionObject)
			{
				case ApplicationException ex:
					ShowError("Произошла ошибка: " + ex.Message, ex);
					break;
				case Exception ex:
					ShowError("Произошла критическая ошибка: " + ex.Message, ex);
					Shutdown();
					break;
				default:
					ShowError("Произошла неожиданное исключение");
					Shutdown();
					break;
			}
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			ShowError("Произошла ошибка: " + e.Exception.Message, e.Exception);
			e.Handled = true; // Предотвращаем завершение приложения
		}

		private void ShowError(string message, Exception? ex = null)
		{
			MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

			if (ex != null)
				_errorWriter.WriteLine(ex);
		}
	}
}