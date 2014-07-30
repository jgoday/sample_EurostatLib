#r "bin/Debug/FSharp.Data.dll"
#r "bin/Debug/EurostatLib.dll"
  
open EurostatLib

// let dsd = DSD("nama_gdp_c")

// dsd.Dimensions |> Seq.iter (fun x -> (printfn "%A %A" x.Id x.Values))
let data = Query("nama_gdp_c", ".EUR_HAB.B1GM.DE+FR+IT", startPeriod = 2010, endPeriod = 2013)
printfn "%A" data.QueryUri
printfn "%A" data.GetData