using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Starfield;
using Noggog;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            displayHelp();
            return;
        }

        string[] parsedArgs = Enumerable.Range(0, 4)
                                         .Select(i => string.Empty)
                                         .ToArray();

        for (int i = 0; i < args.Length; i++)
        {
            if (validateArg(args[i]) == false)
            {
                displayHelp();
                return;
            }

            else
            {
                parsedArgs[i] = args[i];
            }
        }

        if (parsedArgs.Length == 0) {
            displayHelp();
            return;
        }

        Dictionary<string, string> argsDict = validateArgArray(parsedArgs);

        if (argsDict.Count == 0) {
            displayHelp();
            return;
        }

        bool return_value = generateJSON(argsDict["plugin"], argsDict["gen_folder"], false);

        if (return_value == false)
        {
            Console.WriteLine("Something went wrong.");
        }

    }

    // Mutagen stuff
    static bool generateJSON(string esmPath, string generationFolder, bool doOverwrite)
    {
        if (!File.Exists(esmPath))
        {
            Console.WriteLine($"{esmPath}: plugin not found.");
            return false;
        }
        using var mod = StarfieldMod.CreateFromBinaryOverlay(esmPath, StarfieldRelease.Starfield);

        var linkCache = mod.ToImmutableLinkCache();

        string modName = mod.ModKey.ToString().Split(".", 2, StringSplitOptions.None)[0];

        foreach (var context in mod.Armors)
        {
            
            var link = new FormLink<IFormListGetter>(context.FormKey);

            var aaDict = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>();

            string outfitFileName = $"{context.EditorID.ToString()} - {context.FormKey.ToString().Split(":", 2, StringSplitOptions.None)[0]}";

            foreach (var armorAddon in context.Armatures)
            {

                if (linkCache.TryResolve<IArmorAddonGetter>(armorAddon.AddonModel.FormKey, out var aa)) {


                    aaDict.Add(aa.EditorID.ToString(), new Dictionary<string, Dictionary<string, Dictionary<string, string>>>());

                    aaDict[aa.EditorID.ToString()].Add("WorldModel", new Dictionary<string, Dictionary<string, string>>());
                    aaDict[aa.EditorID.ToString()].Add("FirstPersonModel", new Dictionary<string, Dictionary<string, string>>());

                    aaDict[aa.EditorID.ToString()]["WorldModel"].Add("M", new Dictionary<string, string>());
                    aaDict[aa.EditorID.ToString()]["WorldModel"].Add("F", new Dictionary<string, string>());

                    aaDict[aa.EditorID.ToString()]["FirstPersonModel"].Add("M", new Dictionary<string, string>());
                    aaDict[aa.EditorID.ToString()]["FirstPersonModel"].Add("F", new Dictionary<string, string>());


                    try
                    {
                        if (linkCache.TryResolve<IMorphableObjectGetter>(aa.Morphs.Male.WorldMorph.FormKey, out var maleWorldModelMorph))
                        {
                            try
                            {
                                aaDict[aa.EditorID.ToString()]["WorldModel"]["M"].Add("ChargenMorph", maleWorldModelMorph.TCMP);
                            }
                            catch (System.NullReferenceException) { continue; }

                            try
                            {
                                aaDict[aa.EditorID.ToString()]["WorldModel"]["M"].Add("PerformanceMorph", maleWorldModelMorph.TMPP);
                            }
                            catch (System.NullReferenceException) { continue; }
                        }

                        if (linkCache.TryResolve<IMorphableObjectGetter>(aa.Morphs.Male.FirstPersonMorph.FormKey, out var maleFirstPersonMorph))
                        {
                            try
                            {
                                aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["M"].Add("ChargenMorph", maleFirstPersonMorph.TCMP);
                            }
                            catch (System.NullReferenceException) { continue; }

                            try
                            {
                                aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["M"].Add("PerformanceMorph", maleFirstPersonMorph.TMPP);
                            }
                            catch (System.NullReferenceException) { continue; }
                        }

                        if (linkCache.TryResolve<IMorphableObjectGetter>(aa.Morphs.Female.WorldMorph.FormKey, out var femaleWorldModelMorph))
                        {
                            try
                            {
                                aaDict[aa.EditorID.ToString()]["WorldModel"]["F"].Add("ChargenMorph", femaleWorldModelMorph.TCMP);
                            }
                            catch (System.NullReferenceException) { continue; }

                            try
                            {
                                aaDict[aa.EditorID.ToString()]["WorldModel"]["F"].Add("PerformanceMorph", femaleWorldModelMorph.TMPP);
                            }
                            catch (System.NullReferenceException) { continue; }
                        }

                        if (linkCache.TryResolve<IMorphableObjectGetter>(aa.Morphs.Female.FirstPersonMorph.FormKey, out var femaleFirstPersonMorph))
                        {
                            try
                            {
                                aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["F"].Add("ChargenMorph", femaleFirstPersonMorph.TCMP);
                            }
                            catch (System.NullReferenceException) { continue; }

                            try
                            {
                                aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["F"].Add("PerformanceMorph", femaleFirstPersonMorph.TMPP);
                            }
                            catch (System.NullReferenceException) { continue; }
                        }

                        aaDict[aa.EditorID.ToString()]["WorldModel"]["F"].Add("Nif", aa.WorldModel.Female.File);
                        aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["F"].Add("Nif", aa.FirstPersonModel.Female.File);

                        aaDict[aa.EditorID.ToString()]["WorldModel"]["M"].Add("Nif", aa.WorldModel.Male.File);
                        aaDict[aa.EditorID.ToString()]["FirstPersonModel"]["M"].Add("Nif", aa.FirstPersonModel.Male.File);

                    } catch (System.NullReferenceException) { continue; }
                }
            }

            var outfitJSON = new Dto()
            {
                Name = context.EditorID.ToString(),
                FormKey = context.FormKey.ToString().Split(":", 2, StringSplitOptions.None)[0],
                Mod = mod.ModKey.ToString().Split(".", 2, StringSplitOptions.None)[0],
                Race = context.Race.FormKey.ToString().Split(":", 2, StringSplitOptions.None)[0],
                ArmorAddons = aaDict
            };

            var outfitJsonSerialized = JsonSerializer.Serialize(outfitJSON, new JsonSerializerOptions { WriteIndented = true });

            if (File.Exists(Path.Join(generationFolder, modName, outfitFileName)) && doOverwrite == false) {
                Console.WriteLine($"{outfitFileName} already exists, not overwriting...");
                continue;
            }
            else
            {
                if (!Directory.Exists(Path.Join(generationFolder, modName))) {
                    Directory.CreateDirectory(Path.Join(generationFolder, modName));
                }

                File.WriteAllText
                    (
                    Path.Join(generationFolder, modName, $"{outfitFileName}.json"),
                    outfitJsonSerialized
                    );
            }
        }

        return true;
    }

    // Validates individual argument
    static bool validateArg(string arg)
    {
        string[] validArgs = ["plugin", "gen_folder", "overwrite"];

        bool isArgValid = false;

        foreach (string validArg in validArgs)
        {
            if (
                arg.StartsWith(validArg + "=") &&
                arg.Replace(validArg + "=", "") != ""
                )
            {
                isArgValid = true;
                break;
            }
        }

        if (isArgValid == true)
        {
            return true;
        }

        return false;
    }

    // Validates arguments array
    static Dictionary<string, string> validateArgArray(string[] argArray)
    {
        Dictionary<string, string> parsedArgs = new Dictionary<string, string>();

        for (int i = 0; i < argArray.Length; i++)
        {
            if (i >= argArray.Length)
            {
                break;
            }

            string[] parsedArg = argArray[i].ToLower().Split(new string[] { "=" }, 2, StringSplitOptions.None);

            if (parsedArg.Length < 2)
            {
                continue;
            }

            parsedArgs.Add(parsedArg[0], parsedArg[1]);
        }

        if (
            parsedArgs.ContainsKey("plugin") &&
            parsedArgs.ContainsKey("gen_folder")
            )
        {
            if (!File.Exists(parsedArgs["plugin"]))
            {
                Console.WriteLine($"Invalid plugin path: {parsedArgs["plugin"]}");
                return new Dictionary<string, string> { };
            }

            if (!Directory.Exists(parsedArgs["gen_folder"]))
            {
                Console.WriteLine($"Invalid generation folder path: {parsedArgs["gen_folder"]}");
                return new Dictionary<string, string> { };
            }
        }

        else
        {
            Console.WriteLine("Please enter plugin and gen_folder arguments.");
            return new Dictionary<string, string> { };
        }

        foreach (KeyValuePair<string, string> item in parsedArgs)
        {
            Console.WriteLine(item.Key);
            Console.WriteLine(item.Value);
        }

        return parsedArgs;
    }

    // Displays help
    static void displayHelp()
    {
        Console.WriteLine(
            "0.0.1\nGenerates json files with outfit's paths to nifs and morphs.\nUsage:\nplugin=<starfield_plugin>\ngen_folder=<where_to_place_generated_files>\noverwrite=<true/false>"
        );
    }
}

public class Dto
{
    public string Name { get; set; }
    public string FormKey { get; set; }
    public string Mod { get; set; }
    public string Race { get; set; }
    public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> ArmorAddons { get; set; }

}