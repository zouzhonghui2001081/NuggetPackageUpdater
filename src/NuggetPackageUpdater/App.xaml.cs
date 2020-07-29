using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

namespace NuggetPackageUpdater
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			AppDomain.CurrentDomain.UnhandledException += OnAppUnhandledException;
		}

		private void OnAppUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
            var exception = e.ExceptionObject as Exception;
            var message = GetExceptionDetailsString(exception);
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

        private string GetExceptionDetailsString(Exception ex)
        {
            StringBuilder details = new StringBuilder();
            var currentException = ex;
            bool innerException = false;
            while (currentException != null)
            {
                if (innerException)
                    details.Append("\r\n=== Inner Exception ===\r\n");

                details.AppendFormat("{0}\r\n{1}\r\n{2}\r\n", currentException.GetType(), currentException.Message, currentException.StackTrace);
                currentException = currentException.InnerException;
                innerException = true;
            }
            return details.ToString();
        }
    }
}
