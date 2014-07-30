open System
open System.Xml.Linq

open EurostatLib
open EurostatLib.Utils

open FSharp.Charting
open FSharp.Charting.ChartTypes

let testListIndicators() =
    let indicators = new Indicators()
    indicators.Load()

    // Search indicator young immigrants by sex
    indicators.FilterByName("young immigrants") |> List.headAsOption |> printfn "%A"

let testDSD() =
    let indicator = "yth_demo_070"
    let dsd = new DSD(indicator)
    printfn "Uri: %s" dsd.Uri
    for dim in dsd.Dimensions do
        printfn "Dimension: %A" dim

let testData() =
    let parseSerie (serie: EurostatLib.Providers.DataP.Obs) =
        (serie.ObsDimension.Value, serie.ObsValue.Value)

    let getSerieName (serie: EurostatLib.Providers.DataP.SeriesKey) =
        Seq.tryFind (fun (s: EurostatLib.Providers.DataP.Value) -> s.Id = "GEO") serie.Values
            |> Option.map (fun k -> k.Value)
            |> Option.get

    let parseSeries (series: EurostatLib.Providers.DataP.Series[]) =
        seq {
            for s in series ->
                Chart.Line (Seq.map parseSerie s.Obs, Name = getSerieName s.SeriesKey)
        }

    let indicator = "yth_demo_070"
    let dimensions = ".TOTAL.PC.T.Y15-29.ES+IT+PT" //
    let query = Query(indicator, dimensions, startPeriod = 2009, endPeriod = 2013)
    printfn "%s" query.QueryUri
    let data = query.GetData()
    let series = parseSeries data.DataSet.Series
    printfn "%A" series
    let chart = Chart.Combine(series).WithLegend(Enabled = true)

    chart.ShowChart()
    chart.SaveChartAs(@"C:/Tmp/chart.png", ChartImageFormat.Png)


[<EntryPoint>]
let main argv =
    testListIndicators()
    printfn "## TEST DSD ##"
    testDSD()
    printfn "## TEST DATA ##"
    testData()
    0

