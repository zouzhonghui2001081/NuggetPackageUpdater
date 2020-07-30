using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace NuggetPackageUpdater
{
	public class MainWindowViewModel : ViewModelBase
	{
		private string solutionDirectory;
		public string SolutionDirectory
		{
			get
			{
				return solutionDirectory;
			}
			set
			{
				Set(nameof(SolutionDirectory), ref solutionDirectory, value);
			}
		}

		private string packageName;
		public string PackageName
		{
			get
			{
				return packageName;
			}
			set
			{
				Set(nameof(PackageName), ref packageName, value);
			}
		}

		private string packageVersion;
		public string PackageVersion
		{
			get
			{
				return packageVersion;
			}
			set
			{
				Set(nameof(PackageVersion), ref packageVersion, value);
			}
		}

		private ObservableCollection<string> messages;
		public ObservableCollection<string> Messages
		{
			get
			{
				return messages;
			}
			set
			{
				Set(nameof(Messages), ref messages, value);
			}
		}

		public ICommand ReplaceNuggetVersionCommand { get; set; }
		public ICommand BrowseSolutionDirectoryCommand { get; set; }

		public MainWindowViewModel()
		{
			messages = new ObservableCollection<string>();
			ReplaceNuggetVersionCommand = new RelayCommand(ExecuteReplaceNuggetVersionCommand, CanExecuteReplaceNuggetVersionCommand);
			BrowseSolutionDirectoryCommand = new RelayCommand(ExecuteBrowseSolutionDirectoryCommand);
		}

		private void ExecuteBrowseSolutionDirectoryCommand()
		{
			using (var dialog = new FolderBrowserDialog())
			{
				dialog.ShowNewFolderButton = false;
				dialog.Description = "Choose your source code solution directory.";
				dialog.RootFolder = Environment.SpecialFolder.MyComputer;
				DialogResult result = dialog.ShowDialog();
				if (result == DialogResult.OK)
				{
					SolutionDirectory = dialog.SelectedPath;
				}
			}
		}

		private bool CanExecuteReplaceNuggetVersionCommand()
		{
			return !string.IsNullOrWhiteSpace(SolutionDirectory)
				&& !string.IsNullOrWhiteSpace(PackageName)
				&& !string.IsNullOrWhiteSpace(PackageVersion)
				&& Directory.Exists(SolutionDirectory);
		}

		private async void ExecuteReplaceNuggetVersionCommand()
		{
			var allProjectFiles = await GetAllCSProjectFiles(SolutionDirectory);
			if(allProjectFiles == null || !allProjectFiles.Any())
			{
				return;
			}

			Messages.Clear();

			Messages.Add($"{allProjectFiles.Length} csproj file(s) found under {SolutionDirectory}");
			// Update the UI.
			this.DoEvents();

			for(int i = 0; i < allProjectFiles.Length; ++i)
			{
				var projectFile = allProjectFiles[i];
				bool isChanged = await ModifyProjectFileAsync(projectFile, PackageName, PackageVersion);
				if (isChanged)
				{
					var fileName = Path.GetFileName(projectFile);
					Messages.Add($"Version:{PackageVersion} of {packageName} is changed in {fileName} ({projectFile})");
				}
				if(i == allProjectFiles.Length - 1)
				{
					MessageBox.Show("Update the nugget version is done.");
				}
			}
		}

		private async Task<string[]> GetAllCSProjectFiles(string directory)
		{
			var projectFiles = await Task.Factory.StartNew<string[]>(() => { return Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories); });
			return projectFiles;
		}

		private async Task<bool> ModifyProjectFileAsync(string projectFilePath, string packageName, string newVersion)
		{
			bool isChanged = await Task.Factory.StartNew<bool>(() => ModifyProjectFile(projectFilePath, packageName, newVersion));
			return isChanged;
		}

		private bool ModifyProjectFile(string projectFilePath, string packageName, string newVersion)
		{
			bool isModified = false;
			using (var projectCollection = new ProjectCollection())
			{
				var csProject = projectCollection.LoadProject(projectFilePath);
				var packageReferenceGroup = csProject.Xml.ItemGroups.FirstOrDefault(i => i.FirstChild != null && i.FirstChild.ElementName == "PackageReference");
				if (packageReferenceGroup != null)
				{
					var packageToModify = packageReferenceGroup.Items.FirstOrDefault(i => i.Include == packageName);
					if (packageToModify != null)
					{
						foreach (var child in packageToModify.AllChildren)
						{
							if (child is ProjectMetadataElement childToModify && childToModify.Name == "Version")
							{
								childToModify.Value = newVersion;
								isModified = true;
								break;
							}
						}
					}
				}

				if (isModified)
				{
					csProject.Save(projectFilePath);
					return true;
				}
				return false;
			}
		}

		public void DoEvents()
		{
			DispatcherFrame frame = new DispatcherFrame();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
				new DispatcherOperationCallback(ExitFrames), frame);
			Dispatcher.PushFrame(frame);
		}

		public object ExitFrames(object frame)
		{
			((DispatcherFrame)frame).Continue = false;

			return null;
		}
	}
}
