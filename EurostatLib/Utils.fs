namespace EurostatLib

open System

module public Utils =
    type Microsoft.FSharp.Collections.List<'a> with
        member this.HeadAsOption : 'a option =
            if this.IsEmpty then None
            else Some(this.Head)

        static member headAsOption (l: 'a list) : 'a option =
            l.HeadAsOption
