#r "bin/Debug/FSharp.Data.dll"
#r "bin/Debug/EurostatLib.dll"

open EurostatLib
// open Utils

// Get available indicators
let indicators = new Indicators("/Data/Documents/Projects/EurostatLib/EurostatLib/Data/indicators.xml")
indicators.Load()

// indicators.Data |> List.iter (fun x -> printfn "%A" x)


// Search indicator young immigrants by sex
indicators.FilterByName("young immigrants") |> List.headAsOption |> printfn "%A"