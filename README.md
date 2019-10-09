# PathfinderJson
A sheet reader/writer for Pathfinder JSON character sheets from Motokrosh's website
[Take a look at some screenshots!](https://github.com/JaykeBird/PathfinderJson/tree/master/Screenshots)

## How to build
You must have Visual Studio 2019 version 16.3 or later to properly open the project and build it, as it is a .NET Core 3.0 WPF app. As a WPF app, that does mean this is Windows-only. I do wanna experiment with Avalonia later for a cross-platform UI.

Once you open the solution, you'll want to update the UiCore dependency to point towards the UiCore DLL included with the repository. In the future, I'll update the project file so this doesn't have to be manually done. :)

## How to use
This program is just a single EXE file which contains everything. No prerequisites to install, no installer process. Just click on the EXE file and that's it! Do note that it may take a few seconds to open.

Once you have an account on [Mottokrosh's Pathfinder sheet website](charactersheet.co.uk/pathfinder/), you can create character sheets on there. You can download a sheet from the Home page on the website and this program can open, edit, and save the downloaded files! Note that Mottokrosh's website does not appear to have a way to upload sheets

This program supports all the features currently in Mottokrosh's website, and can display them all (and edit most of them) in a sheet editor view. There is also a JSON text editor view for basic raw text editing. (Note that if you want a more full text-editing experience, you may want to open the JSON file in your text editor of choice.)

## To-do List
- Implement Spells tab
- Add "Find" dialog to text editor
- Export functionality (HTML?)
- More options for text editor (font size, no syntax highlighting)
- Central Options dialog

## License
**PathfinderJson is released under the MIT License.**

Do note that the UI library and icon set that PathfinderJson uses are not yet released as open source. The UI library will be released soon under the MIT License, and the full icon set will probably be released in 2020 under some form of Creative Commons license. The icons included in this repository are also released under the MIT license until the full icon set is released. I'll update this section as I go along.
