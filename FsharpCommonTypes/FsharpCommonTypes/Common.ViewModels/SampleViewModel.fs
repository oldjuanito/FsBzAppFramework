﻿namespace Common.ViewModels

open FsharpCommonTypes
open Common.ViewModels.Interfaces

[<CLIMutable>]
type SampleDoc = 
    {
        Name: BusinessTypes.LongNameType; 
        SalesRegion: BusinessTypes.IdNumberType; 
        SalesDate : BusinessTypes.PastDateTimeType;
        QuantityId : BusinessTypes.IdNumberType;
    } 
    with
        static member DefinitionName = 
            { PropDefinition.Name ="Name"; Factory = BusinessTypes.LongName; Setter = (fun d v -> {d with SampleDoc.Name = v }); Getter = (fun d -> d.Name ); 
            PropToInput = BusinessTypes.ToPrimitive  }
        static member DefinitionSalesRegion = 
            { PropDefinition.Name ="Sales Region"; Factory = BusinessTypes.IdNumber; Setter = (fun d v -> {d with SampleDoc.SalesRegion = v }); Getter = (fun d -> d.SalesRegion );
            PropToInput = BusinessTypes.ToPrimitive }  
        static member DefinitionSalesDate = 
            { PropDefinition.Name ="Sales Date"; Factory = BusinessTypes.PastDateTime; Setter = (fun d v -> {d with SampleDoc.SalesDate = v }); Getter = (fun d -> d.SalesDate );
            PropToInput = BusinessTypes.ToPrimitive 
            }
        static member DefinitionQuantityId = 
            { PropDefinition.Name ="Quantity Id"; Factory = BusinessTypes.IdNumberFromStr; Setter = (fun d v -> {d with SampleDoc.QuantityId = v }); Getter = (fun d -> d.QuantityId );
            PropToInput = BusinessTypes.ToPrimitiveStr }  
        interface InterfaceTypes.ICanValidate with 
            member this.GetValidationErrors () = 
                let isObjValid () = 
                    if this.Name = BusinessTypes.LongName "Looper" then  
                        seq [ { PropertyError.ErrorCode ="YY"; Description ="TestError"; PropertyName=""; } ] 
                    else 
                        Seq.empty
                [ SampleDoc.DefinitionName.GetValidationErrors(this) ; 
                   SampleDoc.DefinitionSalesRegion.GetValidationErrors(this);
                   SampleDoc.DefinitionSalesDate.GetValidationErrors(this);
                   SampleDoc.DefinitionQuantityId.GetValidationErrors(this);
                   isObjValid(); ] 
                |> Seq.collect id
                
[<CLIMutable>]
type SampleDocReadOnly = 
    {Name: string; SalesRegion: int; SalesDate : Option<System.DateTime>} 
[<CLIMutable>]
type SampleDocList = 
    {Docs: SampleDocReadOnly seq; SelectedItem: Option<SampleDocReadOnly>} 
    with
        interface InterfaceTypes.ICanValidate with 
            member this.GetValidationErrors () = Seq.empty

module Sample =
    open System.Threading.Tasks
    let CreateSampleDoc () =
        let now = System.DateTime.Now
        let model ={SampleDoc.Name= BusinessTypes.LongName "Alabama" ; SalesRegion = BusinessTypes.IdNumber 1; SalesDate =  BusinessTypes.PastDateTime now; QuantityId = BusinessTypes.IdNumber -1} 
        model
    let DoNothingCmd  doc =
        async {

            return { CommandResult.Errors = Seq.empty; CommandResult.Message = "Thanks!" }
        }
    let SampleCmd screenId (dialogService:IDialogService) doc =
        async {
            let! resp =  (dialogService.PromptMessage screenId "Test" "Test")
                        |> Async.AwaitIAsyncResult 

            return { CommandResult.Errors = Seq.empty; CommandResult.Message = "Thanks!" }
        }
    let BuildViewModels afterSuccess model  =
        let simpleChoices (doc:SampleDoc) = 
                                let newRand = System.Random().Next(1,6).ToString()
                                [ {ResultId= 1; ResultLabel= "Test 1";  };
                                {ResultId= 2; ResultLabel= "Test " ;  } ;
                                {ResultId= 3; ResultLabel= "Test 3 " + doc.Name.ToString();  }  ]
        let cmd = { CommandDefinition.CmdName = "Save"; CommandDefinition.CmdExecuter = DoNothingCmd}
        let cancelCmd = CommandDefinition.CancelCmdDefinition
        let afterCancel = afterSuccess
        let doc = DocViewModel(model, cmd, afterSuccess,cancelCmd, afterCancel, Seq.empty)
        SingleInputViewModel.AddTextInputViewModel doc (doc.GetRootView()) SampleDoc.DefinitionName 
        SingleInputViewModel.AddMaskedTextInputViewModel doc (doc.GetRootView()) SampleDoc.DefinitionQuantityId "999999990"
        SingleInputViewModel.AddDateInputViewModel doc (doc.GetRootView()) SampleDoc.DefinitionSalesDate 
        SimpleChoicesViewModel.AddSimpleChoicesViewModel doc (doc.GetRootView()) SampleDoc.DefinitionSalesRegion simpleChoices 
        doc
    let CreateSampleEditScreen (screenManager:ScreenManager) =
        let screenName = "Sales Person" 
        let screenId = CommandScreen.GenerateId screenName
        let afterSuccess doc cmdResult =
            screenManager.RemoveScreen screenId
            ()
            
        let afterFailure doc cmdResult =
            ()
        let screen = CommandScreen.CreateScreen CreateSampleDoc (BuildViewModels afterSuccess) screenName screenId
        screenManager.AddScreen screen // TODO should this be here or bootstrapper??
        (screen :> IScreen).Init()
        screen
        
    let CreateSampleListDoc () =
        let now =  Some System.DateTime.Now
        { 
            SampleDocList.Docs =
                [
                    {Name=  "Alabama" ; SalesRegion =   1; SalesDate =    now} 
                    {Name=  "Alabama" ; SalesRegion =  2; SalesDate =    now} 
                    {Name=  "Colorado" ; SalesRegion =  3; SalesDate =    now} 
                    {Name=  "California" ; SalesRegion = 4; SalesDate =    now} 
                ]
            SelectedItem = None
        }
        
    let BuildListViewModels (dialogService:IDialogService) afterSuccess screenId model  =
        
        let cmd = { CommandDefinition.CmdName = "Edit"; CommandDefinition.CmdExecuter = SampleCmd screenId dialogService }
        let cancelCmd = CommandDefinition.CancelCmdDefinition
        let afterCancel = afterSuccess

        let doc = DocViewModel(model, cmd, afterSuccess,cancelCmd, afterCancel, Seq.empty)
        let loadList doc = doc.Docs
        let onSelectedItem doc item = 
            {doc with SelectedItem = item}
        let pivotSettings = {
            PivotGridDefinition.RowDimensionDefinitions  = 
                [
                   PivotGridDefinition.CreatePivotDimension "Sales Region" "SalesRegion"
                ]
                
            PivotGridDefinition.ColumnDimensionDefinitions = 
                [
                   PivotGridDefinition.CreatePivotDimension "Name" "Name"
                ]
                
            PivotGridDefinition.FactDefinitions = 
                [
                   PivotGridDefinition.CreateFactDimension "Sales Date" "SalesDate" PivotGridDefinition.FactTypes.DateFact
                ]
            }
        let pivotDef = { PivotSettings = pivotSettings; RefreshValFromDoc = loadList; SelectedItemSetter = onSelectedItem; PropName ="Docs" }
        PivotGridViewModel.AddPivotGridViewModel doc (doc.GetRootView())  pivotDef
        doc

    let CreateSampleListScreen (dialogService:IDialogService) (screenManager:ScreenManager) =
        let screenName = "List Sales People" 
        let screenId = CommandScreen.GenerateId screenName
        let afterSuccess doc cmdResult =
            CreateSampleEditScreen screenManager |> ignore
            ()
            
        let afterFailure doc cmdResult =
            ()
        let screen = CommandScreen.CreateScreen CreateSampleListDoc (BuildListViewModels dialogService afterSuccess screenId) screenName screenId
        screenManager.AddScreen screen // TODO should this be here or bootstrapper??
        (screen :> IScreen).Init()
        screen