## Contributing

The mod has the following guidelines:

- No feature should require spawning an object or require creating complex objects in the world.
- Do not try to fix social issues with code.
- Cleanup after injecting code or objects. Leave no trace behind. Always make things non-persistent.

## Compiling the mod from source

Building the mod from source is easy. You'll need to install the latest .NET SDK (9.0 at the time of writing) and Git.

1. Clone this repository with git and navigate into the project folder (the one containing the csproj file, not the sln file)
2. Create a copy of the file "Directory.Build.props.template" and remove the ".template" from the name so the copied file is called `Directory.Build.props`
3. Open that copied file and change the Resonite install path if necessary. Make sure it ends with a backslash, or you will get build errors.
4. Run `dotnet build -c Release` in the project folder to compile the mod. It should take about 10 seconds, and when it completes, the mod dll will be automatically copied to your `rml_mods` folder.

And that's it! If you go to compile the mod again at a later date, don't forget to pull the latest changes from git before you recompile.

<details>
<summary>I need a step-by-step walkthrough on how to do this (click to expand)</summary>

In order to compile the mod, you will need three things: Resonite, Git (or a graphical Git client), and the latest .NET SDK.

If this is the first time you are compiling the mod, start from step 1. Otherwhise, if you are re-compiling the mod after an update, start from step 5.

**1.** Install the latest .NET SDK.
- At the time of writing, that is .NET 9.0.
- Visit [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download), click "Download .NET SDK" under the .NET 9.0 box, and run the downloaded installer.

**2.** Install git
- This guide assumes you are working with git on the command line.
- Visit [https://git-scm.com/downloads/win](https://git-scm.com/downloads/win), click the first link on page ("Click here to download"), and run the installer.

**3.** Clone this repository
- Find a folder on your PC to download the mod files to. In that folder (or on your desktop), hold shift and right-click. If you see the option "Open in Terminal", click that. Otherwise, pick "Open PowerShell window here."
- Copy this command: `git clone https://github.com/SnepDrone/Restrainite.git`
- In the shell window that opened, *RIGHT-CLICK* to paste the command. Ctrl-V will NOT work!
- Verify that what got pasted matches the command above, then hit enter to run it.
- Once the command has completed (your cursor is returned to a prompt), you may close the window.

**4.** Open the downloaded files and set your Resonite path
- Back in file explorer, open the newly downloaded folder named Restrainite. In that folder is yet another folder also named Restrainite. Open that second folder.
- Find the file named "Directory.Build.props.template" and create a copy of it. Remove the ".template" from the name so the copied file is called `Directory.Build.props`
- **Stop.** If you installed Resonite in the default steamapps directory on your C: drive, you can move ahead to step 5. Otherwise, if Resonite is installed in a non-standard location (or you are compiling this mod on Linux), proceed to step 4.5.

**4.5.** Find the full path to your Resonite install
- Open the copied file from the previous step in a text editor like Notepad.
- Open Steam, right-click Resonite in the games list, hover the "Manage" option, and select "Browse local files" in the submenu.
- A file explorer window will open. Without clicking anything, press Ctrl-L to focus and select the folder path, then press Ctrl-C to copy it to your clipboard.
- Return to your text editor with Directory.Build.Props open, and replace the pre-filled folder path with the path you copied. Make sure the path ends with a backslash (`\`) character.
- Save the file and close the text editor, proceed to step 5.

**5.** Compile the damn mod finally!
- In the project folder (the same one you copied the file in), shift-right-click again and open a terminal window.
- Unless you just started at step 1 and cloned the mod source fresh, pull the latest updates by running the command `git pull`
- Finally, run `dotnet build -c Release` to compile the mod. If all went well, it should complete within 10 seconds without any errors.
- If it succeeded, great! The mod dll was automatically copied to your `rml_mods` folder.