# PathfinderJson
![Logo](Icon96.png?raw=true "Logo")

A sheet reader/writer for Pathfinder JSON character sheets from Mottokrosh's website

[Take a look at some screenshots!](https://github.com/JaykeBird/PathfinderJson/wiki/Screenshots)

## How to "install"
PathfinderJson requires Windows 7 with SP1, Windows 8.1, or Windows 10 with Anniversary Update or later. As long as you've installed all your updates, you should be good. You should be just able to double-click the EXE file in the zip folder you download and it'll open right up. There is no actual installation process, just download and go. (This also means this program is portable as well!)

For **64-bit** computers, use the x64 download. For **32-bit** computers, use the x86 download. If you're not sure what you have, you probably have a 64-bit computer, but you can go to [WhatsMyOS.com](http://whatsmyos.com/) to get a better idea.

### Issues running the program?

If it does not open or an error occurs (may take a few moments), you may need to install the Visual Studio C++ Redistributable package. I've included this for your convenience in the MSVC folder here, and in the downloads for version 0.9.1 and later. See [Microsoft's website](https://www.microsoft.com/en-us/download/details.aspx?id=52685) for more details. If there's still problems, make sure you have all updates installed; see the purple Note box on [this Microsoft webpage](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x#net-core-dependencies) for more details.

If you're still having troubles - especially if it's a situation where it opens once or twice but then no longer opens - you may have to disable the Startup Optimization feature. I'll write a proper help page about this later, but if you have this issue, try downloading the 1.1.1 release, opening it and going to "Tools > Options". On the Advanced tab, turn off the "Use Startup Optimization" feature. Click OK and then close the program. You should now be able to go back to the latest release and use it just fine. Reach me to me if you have any issues or difficulties with this process!

You can reach out to me if you have any questions by [finding me on Twitter](https://twitter.com/JaykeBird) or [opening an issue report](https://github.com/JaykeBird/PathfinderJson/issues/new/choose) here on GitHub.

## How to use
To get started, you can either create a new character from the File menu, or you can open a file by selecting File > Open or dropping the JSON file into the program. You can create, view, and edit sheets.

You can put in JSON files created using this program, or (if you have an account on [Mottokrosh's Pathfinder sheet website](http://charactersheet.co.uk/pathfinder/)) you can download character sheets from the Home page of Mottokrosh's website. Note that Mottokrosh's website does not appear to have a way to upload sheets back to the site.

You have three ways to view a file:
- a tabbed sheet view, where all the info is laid out via different tabs
- a continuous scroll view, where the info is still laid out but you can scroll through all of it (most similar to how Mottokrosh's site looks)
- a JSON text editor view, for basic raw text editing

This program supports all the features currently in Mottokrosh's website, and can display and edit nearly all of them in the sheet and scroll views.

## How to build
You must have Visual Studio 2019 version 16.3 or later to properly open the project and build it, as it is a .NET Core 3.0 WPF app. As a WPF app, that does mean this is Windows-only. I do wanna experiment with Avalonia later for a cross-platform UI.

Fun note: the main window seems to lag Visual Studio pretty bad, due to how many controls that's in it (especially after I added in the Spells tab and its 20+ text boxes). Although it is annoying, it hasn't bothered me to the point that I've done much about it though. This may be more of an issue for a computer that isn't the most powerful; my machine is about a mid-tier build.

## Roadmap
View this project's roadmap [on the GitHub wiki page](https://github.com/JaykeBird/PathfinderJson/wiki/Roadmap).

## IntelliCode
If you're using IntelliCode in Visual Studio 2019 (or Visual Studio 2017 15.8 or later), I have an IntelliCode model that I have been using. If you're looking to fork this project and want to utilize my IntelliCode model, I'll provide you the link to it below:

IntelliCode model: https://prod.intellicode.vsengsaas.visualstudio.com/get?m=509ED9C446024BFF8A4F15CD1D3B576B

How to add the model to your fork of the code: [View page on Microsoft Docs site](https://docs.microsoft.com/en-us/visualstudio/intellicode/share-models#add-a-custom-model)

## License
**PathfinderJson is released under the [MIT License](License.md).**

Do note that the UI library and icon set that PathfinderJson uses are not yet released as open source. The UI library will be released soon under the MIT License, and the full icon set will probably be released in 2020 under some form of Creative Commons license. The icons included in this repository are also released under the MIT license until the full icon set is released. I'll update this section as I go along.

This program complies with the [Paizo Community Use Policy](https://paizo.com/community/communityuse). More detail can be found on the Paizo website or in the Help > About window.

For more info about the third-party libraries used in this project, open the Help > About > Third-Party Credits window.