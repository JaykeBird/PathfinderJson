# PathfinderJson
![Logo](Icon48.png?raw=true "Logo")

A sheet reader/writer for Pathfinder JSON character sheets from Motokrosh's website

[Take a look at some screenshots!](https://github.com/JaykeBird/PathfinderJson/tree/master/Screenshots)

## How to build
You must have Visual Studio 2019 version 16.3 or later to properly open the project and build it, as it is a .NET Core 3.0 WPF app. As a WPF app, that does mean this is Windows-only. I do wanna experiment with Avalonia later for a cross-platform UI.

## How to install
PathfinderJson requires Windows 7 with SP1, Windows 8.1, or Windows 10 with Anniversary Update or later. As long as you've installed all your updates, you should be good. You should be just able to double-click the EXE file in the zip folder you download and it'll open right up.

For **64-bit** computers, use the x64 download. For **32-bit** computers, use the x86 download. If you're not sure what you have, you probably have a 64-bit computer, but you can go to [WhatsMyOS.com](http://whatsmyos.com/) to get an idea of what you have.

If it does not open or an error occurs (may take a few moments), you may need to install the Visual Studio C++ Redistributable package. I've included this for your convenience in the MSVC folder here, and in version 0.9.1 downloads and later. See [Microsoft's website](https://www.microsoft.com/en-us/download/details.aspx?id=52685) for more details. If there's still problems, make sure you have all updates installed; see the purple Note box on [this Microsoft webpage](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x#net-core-dependencies) for more details.

You can reach out to me if you have any questions by [finding me on Twitter](https://twitter.com/JaykeBird) or [opening an issue report](https://github.com/JaykeBird/PathfinderJson/issues/new/choose) here on GitHub.

## How to use
This program is just a single EXE file which contains everything. No prerequisites to install, no installer process. Just click on the EXE file and that's it! Do note that it may take a few seconds to open.

Once you have an account on [Mottokrosh's Pathfinder sheet website](charactersheet.co.uk/pathfinder/), you can create character sheets on there. You can download a sheet from the Home page on the website and this program can open, edit, and save the downloaded files! Note that Mottokrosh's website does not appear to have a way to upload sheets

This program supports all the features currently in Mottokrosh's website, and can display them all (and edit most of them) in a sheet editor view. There is also a JSON text editor view for basic raw text editing. (Note that if you want a more full text-editing experience, you may want to open the JSON file in your text editor of choice.)

## To-do List
- Export/printing functionality (HTML?)
- More options for text editor (font size, no syntax highlighting)
- Auto-complete spell list
- More connections to [D20PFSRD](https://d20pfsrd.com)
- Central Options dialog
- Dice roller tool
- Import from other tools? (AwesomeSheet, Hero Lab)

## IntelliCode
If you're using IntelliCode in Visual Studio 2019 (or Visual Studio 2017 15.8 or later), I have an IntelliCode model that I have been using. If you're looking to fork this project and want to utilize my IntelliCode model, I'll provide you the link to it below:

IntelliCode model: https://prod.intellicode.vsengsaas.visualstudio.com/get?m=EE624218283041248FCC4F9108367CB4

How to add the model to your fork of the code: [View page on Microsoft Docs site](https://docs.microsoft.com/en-us/visualstudio/intellicode/share-models#add-a-custom-model)

## License
**PathfinderJson is released under the MIT License.**

Do note that the UI library and icon set that PathfinderJson uses are not yet released as open source. The UI library will be released soon under the MIT License, and the full icon set will probably be released in 2020 under some form of Creative Commons license. The icons included in this repository are also released under the MIT license until the full icon set is released. I'll update this section as I go along.
