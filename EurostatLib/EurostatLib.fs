namespace EurostatLib

open System
open FSharp.Data

open Utils

// How to build a REST query to retrieve Eurostat data
// http://epp.eurostat.ec.europa.eu/portal/page/portal/sdmx_web_services/getting_started/a_few_useful_points

// Data Providers
module Providers =
    [<Literal>]
    let indicatorsUri = "http://ec.europa.eu/eurostat/SDMX/diss-web/rest/dataflow/ESTAT/all/latest"
    [<Literal>]
    let dsdUri = "http://ec.europa.eu/eurostat/SDMX/diss-web/rest/datastructure/ESTAT/DSD_nama_gdp_c"
    [<Literal>]
    let dataUri = "http://ec.europa.eu/eurostat/SDMX/diss-web/rest/data/nama_gdp_c/.EUR_HAB.B1GM.DE+FR+IT"

    type IndicatorsP = XmlProvider<indicatorsUri>
    type DSDP = XmlProvider<dsdUri>
    type DataP = XmlProvider<dataUri>

type Indicator(id: string, name: string) =
    member this.Name = name
    member this.Id = id

    override this.ToString() = String.Format("{0} - {1}", this.Id, this.Name)

// Available values from a dimension
//  Example : "FREQ" [[D] Daily; [W] Weekly; [Q] Quarterly; [A] Annual; ..
type DimensionValue(id: string, name: string) =
    member this.Id = id
    member this.Name = name

    override this.ToString() = String.Format("[{0}] {1}", this.Id, this.Name)

// Dimension of an indicator filter
// Example : [ FREQ, UNIT, INDIC_NA, GEO ]
type Dimension(id: string, values: list<DimensionValue>) =
    member this.Id = id
    member this.Values = values

    override this.ToString() =
        let valuesStr = String.Join(", ", this.Values)
        String.Format("Id = {0}, Values = {1}", this.Id, valuesStr)

// Retrieve the Data Structure Definition (DSD) related to an indicator
type DSD(indicatorName: string) =

    let uri = "http://ec.europa.eu/eurostat/SDMX/diss-web/rest/datastructure/ESTAT/DSD_" + indicatorName
    let ExtractDescription(data: Providers.DSDP.Code) =
        data.Name.Value

    let ParseCode(c: Providers.DSDP.Code) =
        DimensionValue(c.Id.Value, ExtractDescription(c))

    let ExtractCodeList(id: string, codeList: Providers.DSDP.Codelists) =
        codeList.Codelists
            |> Seq.filter (fun c -> c.Id = "CL_" + id)
            |> Seq.head
            |> (fun x -> x.Codes)
            |> Seq.map ParseCode
            |> Seq.toList

    let ExtractDimension(d: Providers.DSDP.DimensionList, codeList: Providers.DSDP.Codelists) =
        d.Dimensions |> Seq.map (fun x -> Dimension(x.Id, ExtractCodeList(x.Id, codeList)))

    let data = Providers.DSDP.Load uri

    let dimensions =
        data.Structures.DataStructures.DataStructure.DataStructureComponents.DimensionList
            |> (fun x -> ExtractDimension(x, data.Structures.Codelists))

    member this.Code = indicatorName
    member this.Uri = uri
    member this.Dimensions = dimensions

    static member Load(code: string) =
        DSD(code)

    static member LoadAsync(code: string) = async {
        let dsd = DSD(code)

        return dsd
    }

type Query(indicatorCode: string, dimensions: string, ?startPeriod: int, ?endPeriod: int) =
    let baseUri = "http://ec.europa.eu/eurostat/SDMX/diss-web/rest/data/" + indicatorCode
    let parsePeriod(name: string, i: option<int>) =
        defaultArg (Option.map (fun p -> System.String.Format("{0}={1}", name, p)) i) ""
    
    let questionMark = if (startPeriod.IsSome || endPeriod.IsSome) then "?" else ""
    let andMark = if (startPeriod.IsSome && endPeriod.IsSome) then "&" else ""
    let timeFilter =
        System.String.Format(
            "{0}{1}{2}{3}",
            questionMark,
            parsePeriod("startPeriod", startPeriod),
            andMark,
            parsePeriod("endPeriod", endPeriod))

    let queryUri =
        baseUri + "/" + dimensions + timeFilter

    let data = lazy (Providers.DataP.Load queryUri)

    member this.QueryUri = queryUri
    member this.GetData() = data.Force()

// Get the available Indicators
type Indicators(?resourceName: string) =
    let mutable data: Indicator list = []
    let resourceName = defaultArg resourceName Providers.indicatorsUri
    let ExtractDescription(data: Providers.IndicatorsP.Dataflows) =
        data.Names
        |> Seq.filter (fun x -> x.Lang = "en")
        |> Seq.map (fun x -> x.Value)
        |> Seq.head
    
    let ParseIndicators(data: Providers.IndicatorsP.Structure) =
        data.Structures.Dataflows
        |> Seq.map (fun x -> Indicator(x.Id, ExtractDescription(x)))
        |> Seq.toList
    
    member this.Data =
        data

    member this.Load() =
        data <- Providers.IndicatorsP.Load resourceName |> ParseIndicators

    member this.LoadAsync() = async {
        let! d = Providers.IndicatorsP.AsyncLoad resourceName
        data <- d |> ParseIndicators
    }

    member this.FilterByName(name: string): Indicator list =
        data |> List.filter (fun i -> i.Name.ToLower().Contains(name.ToLower()))

    member this.FilterById(id: string): Indicator option =
        data |> List.filter (fun i -> i.Id = id) |> List.headAsOption