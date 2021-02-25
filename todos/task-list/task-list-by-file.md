## AasxAmlImExport\AmlExport.cs

[Line 871, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlExport.cs#L871
), 
Michael Hoffmeister,
2020-08-01

    If further data specifications exist (in future), add here

## AasxAmlImExport\AmlImport.cs

[Line 169, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlImport.cs#L169
), 
MIHO,
2020-08-01

    The check for role class or requirements is still questionable
    but seems to be correct (see below)
    
    Question MIHO: I dont understand the determinism behind that!
    WIEGAND: me, neither ;-)
    Wiegand:  ich hab mir von Prof.Drath nochmal erklären lassen, wie SupportedRoleClass und
    RoleRequirement verwendet werden:
    In CAEX2.15(aktuelle AML Version und unsere AAS Mapping Version):
      1.Eine SystemUnitClass hat eine oder mehrere SupportedRoleClasses, die ihre „mögliche Rolle
        beschreiben(Drucker / Fax / kopierer)
      2.Wird die SystemUnitClass als InternalElement instanziiert entscheidet man sich für eine
        Hauptrolle, die dann zum RoleRequirement wird und evtl. Nebenklassen die dann
        SupportedRoleClasses sind(ist ein Workaround weil CAEX2.15 in der Norm nur
        ein RoleReuqirement erlaubt)
    InCAEX3.0(nächste AMl Version):
      1.Wie bei CAEX2.15
      2.Wird die SystemUnitClass als Internal Elementinstanziiert werden die verwendeten Rollen
        jeweils als RoleRequirement zugewiesen (in CAEX3 sind mehrere RoleReuqirements nun erlaubt)

[Line 1436, column 45](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxAmlImExport/AmlImport.cs#L1436
), 
Michael Hoffmeister,
2020-08-01

    fill out 
    eds.hasDataSpecification by using outer attributes

## AasxCsharpLibrary.Tests\TestLoadSaveChain.cs

[Line 42, column 5](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L42
), 
mristin,
2020-10-05

    The class is unused since all its tests were disabled temporarily and
    will be fixed in the near future.
    
    Once the tests are enabled, please remove this Resharper directive.

[Line 92, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L92
), 
mristin,
2020-10-05

    This test has been temporary disabled so that we can merge in the branch
    MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
    again.
    
    Please do not forget to remove the Resharper directive at the top of this class.
    
    [TestCase(".xml")]
    
    dead-csharp ignore this comment

[Line 138, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L138
), 
mristin,
2020-10-05

    This test has been temporary disabled so that we can merge in the branch
    MIHO/EnhanceDocumentShelf. The test should be fixed in a future pull request and we will then re-enable it
    again.
    
    Please do not forget to remove the Resharper directive at the top of this class.
    
    [Test]
    
    dead-csharp ignore this comment

[Line 163, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary.Tests/TestLoadSaveChain.cs#L163
), 
mristin,
2020-09-17

    Remove autofix once XSD and Aasx library in sync
    
    Package has been loaded, now we need to do an automatic check & fix.
    
    This is necessary as Aasx library is still not conform with the XSD AASX schema and breaks
    certain constraints (*e.g.*, the cardinality of langString = 1..*).

## AasxCsharpLibrary\AasxCompatibilityModels\V10\AdminShellV10.cs

[Line 1843, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L1843
), 
Michael Hoffmeister,
1970-01-01

    in V1.0, shall be a list of embeddedDataSpecification

[Line 2561, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L2561
), 
Michael Hoffmeister,
1970-01-01

    Qualifiers not working!

[Line 2921, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L2921
), 
Michael Hoffmeister,
1970-01-01

    Operation

[Line 3900, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L3900
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 3925, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L3925
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4032, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4032
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4061, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4061
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

[Line 4088, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AasxCompatibilityModels/V10/AdminShellV10.cs#L4088
), 
Michael Hoffmeister,
1970-01-01

    use aasenv serialzers here!

## AasxCsharpLibrary\AdminShell.cs

[Line 1207, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L1207
), 
MIHO,
2020-08-30

    this does not prevent the corner case, that we could have
    * multiple dataSpecificationIEC61360 in this list, which would be an error

[Line 2951, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L2951
), 
MIHO,
2020-08-27

    According to spec, cardinality is [1..1][1..n]

[Line 2955, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L2955
), 
MIHO,
2020-08-27

    According to spec, cardinality is [0..1][1..n]

[Line 2986, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L2986
), 
MIHO,
2020-08-27

    According to spec, cardinality is [0..1][1..n]

[Line 3265, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3265
), 
MIHO,
2020-08-30

    align wording of the member ("embeddedDataSpecification") with the 
    * wording of the other entities ("hasDataSpecification")

[Line 3966, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L3966
), 
MIHO,
2020-08-26

    not very elegant, yet. Avoid temporary collection

[Line 4646, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L4646
), 
Michael Hoffmeister,
2020-08-01

    check, if Json has Qualifiers or not

[Line 5423, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShell.cs#L5423
), 
MIHO,
2020-07-31

    would be nice to use IEnumerateChildren for this ..

## AasxCsharpLibrary\AdminShellPackageEnv.cs

[Line 268, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L268
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 454, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L454
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 497, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L497
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 532, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L532
), 
Michael Hoffmeister,
2020-08-01

    use a unified function to create a serializer

[Line 586, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellPackageEnv.cs#L586
), 
MIHO,
2021-01-02

    check again.
    * Revisiting this code after a while, and after
    * the code has undergo some changes by MR, the following copy command needed
    * to be amended with a if to protect against self-copy.

## AasxCsharpLibrary\AdminShellUtil.cs

[Line 212, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxCsharpLibrary/AdminShellUtil.cs#L212
), 
MIHO,
2020-11-12

    replace with Regex for multi language. Ideally have Exception messages
    always as English.

## AasxDictionaryImport.Tests\Cdd\TestImport.cs

[Line 83, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestImport.cs#L83
), 
Robin,
2020-09-03

    please check

[Line 99, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestImport.cs#L99
), 
Robin,
2020-09-03

    please check

## AasxDictionaryImport.Tests\Cdd\TestModel.cs

[Line 555, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport.Tests/Cdd/TestModel.cs#L555
), 
krahlro-sick,
2020-07-31

    make sure that there are no duplicates

## AasxDictionaryImport\Eclass\Model.cs

[Line 394, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Eclass/Model.cs#L394
), 
krahlro-sick,
2021-02-03

    HTML-decode SI code

[Line 819, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Eclass/Model.cs#L819
), 
krahlro-sick,
2021-02-23

    This logic is copied from EclassUtils.GenerateConceptDescription -- does

## AasxDictionaryImport\Iec61360Utils.cs

[Line 126, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L126
), 
Robin,
2020-09-03

    MIHO is not sure, if the data spec reference is correct; please check

[Line 142, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L142
), 
Robin,
2020-09-03

    MIHO is not sure, if the data spec reference is correct; please check

[Line 158, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxDictionaryImport/Iec61360Utils.cs#L158
), 
Robin,
2020-09-03

    check this code

## AasxPackageExplorer.Tests\TestOptionsAndPlugins.cs

[Line 173, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPackageExplorer.Tests/TestOptionsAndPlugins.cs#L173
), 
mristin,
2020-11-13

    @MIHO please check -- Options should be null, not empty?

## AasxPluginPlotting\PlottingViewControl.xaml.cs

[Line 225, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginPlotting/PlottingViewControl.xaml.cs#L225
), 
MIHO,
2021-01-04

    consider at least to include MLP, as well

## AasxPluginUaNetClient\UASampleClient.cs

[Line 1, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginUaNetClient/UASampleClient.cs#L1
), 
MIHO,
2020-08-06

    lookup SOURCE!

## AasxPluginWebBrowser\Plugin.cs

[Line 144, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxPluginWebBrowser/Plugin.cs#L144
), 
MIHO,
2020-08-02

    when dragging the divider between elements tree and browser window,

## AasxSignature\AasxSignature.cs

[Line 30, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L30
), 
Andreas Orzelski,
2020-08-01

    The signature file and [Content_Types].xml can be tampered?

[Line 180, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L180
), 
Andreas Orzelski,
2020-08-01

    Is package according to the Logical model of the AAS?

[Line 214, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L214
), 
Andreas Orzelski,
2020-08-01

    is package sealed? => no other signatures can be added?

[Line 217, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxSignature/AasxSignature.cs#L217
), 
Andreas Orzelski,
2020-08-01

    The information from the analysis
    -> return as an object (list of enums with the issues/warings???)

## AasxToolkit.Tests\TestProgram.cs

[Line 251, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxToolkit.Tests/TestProgram.cs#L251
), 
mristin,
2020-10-30

    add json once the validation is in place.
     Michael Hoffmeister had it almost done today.
     
    Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "TestResources\\AasxToolkit.Tests\\sample.json")
        
        dead-csharp ignore this comment

## AasxUaNetConsoleServer\Program.cs

[Line 10, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetConsoleServer/Program.cs#L10
), 
MIHO,
2020-08-03

    check SOURCE

## AasxUaNetServer\AasxServer\AasEntityBuilder.cs

[Line 280, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasEntityBuilder.cs#L280
), 
MIHO,
2020-08-06

    check, which namespace shall be used

## AasxUaNetServer\AasxServer\AasUaEntities.cs

[Line 20, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L20
), 
MIHO,
2020-08-29

    The UA mapping needs to be overworked in order to comply the joint aligment with I4AAS

[Line 21, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L21
), 
MIHO,
2020-08-29

    The UA mapping needs to be checked for the "new" HasDataSpecification strcuture of V2.0.1

[Line 685, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L685
), 
MIHO,
2020-08-06

    check (again) if reference to CDs is done are shall be done

[Line 976, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L976
), 
MIHO,
2020-08-06

    not sure if to add these

[Line 1077, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1077
), 
MIHO,
2020-08-06

    use the collection element of UA?

[Line 1393, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1393
), 
MIHO,
2020-08-06

    decide to from where the name comes

[Line 1396, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1396
), 
MIHO,
2020-08-06

    description: get "en" version which is appropriate?

[Line 1399, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1399
), 
MIHO,
2020-08-06

    parse UA data type out .. OK?

[Line 1408, column 33](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1408
), 
MIHO,
2020-08-06

    description: get "en" version is appropriate?

[Line 1417, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1417
), 
MIHO,
2020-08-06

    this any better?

[Line 1421, column 37](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1421
), 
MIHO,
2020-08-06

    description: get "en" version is appropriate?

[Line 1765, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/AasxServer/AasUaEntities.cs#L1765
), 
MIHO,
2020-08-06

    check, if to make super classes for UriDictionaryEntryType?

## AasxUaNetServer\Base\SampleNodeManager.cs

[Line 666, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/Base/SampleNodeManager.cs#L666
), 
MIHO,
2020-08-06

    check, if this is valid use of the SDK. MIHO added this

## AasxUaNetServer\SampleServer.cs

[Line 173, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUaNetServer/SampleServer.cs#L173
), 
MIHO,
2020-08-04

    To be checked by Andreas. All applications have software certificates

## AasxUANodesetImExport\UANodeSet.cs

[Line 24, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSet.cs#L24
), 
Michael Hoffmeister,
2020-08-01

    Fraunhofer IOSB: Check ReSharper to be OK

## AasxUANodesetImExport\UANodeSetExport.cs

[Line 30, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetExport.cs#L30
), 
Michael Hoffmeister,
1970-01-01

    License

[Line 31, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetExport.cs#L31
), 
Michael Hoffmeister,
1970-01-01

    Fraunhofer IOSB: Check ReSharper

## AasxUANodesetImExport\UANodeSetImport.cs

[Line 27, column 1](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxUANodesetImExport/UANodeSetImport.cs#L27
), 
Michael Hoffmeister,
2020-08-01

    Fraunhofer IOSB: Check ReSharper settings to be OK

## AasxWpfControlLibrary\DiplayVisualAasxElements.xaml.cs

[Line 434, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DiplayVisualAasxElements.xaml.cs#L434
), 
MIHO,
2021-01-04

    check to replace all occurences of RefreshFromMainData() by
    * making the tree-items ObservableCollection and INotifyPropertyChanged

[Line 753, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DiplayVisualAasxElements.xaml.cs#L753
), 
MIHO,
2020-07-21

    was because of multi-select

## AasxWpfControlLibrary\DispEditAasxEntity.xaml.cs

[Line 1550, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditAasxEntity.xaml.cs#L1550
), 
MIHO,
2020-09-01

    extend the lines below to cover also data spec. for units

## AasxWpfControlLibrary\DispEditHelperBasics.cs

[Line 1304, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditHelperBasics.cs#L1304
), 
Michael Hoffmeister,
2020-08-01

    possibly [Jump] button??

[Line 1484, column 25](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditHelperBasics.cs#L1484
), 
Michael Hoffmeister,
2020-08-01

    Needs to be revisited

## AasxWpfControlLibrary\DispEditHelperCopyPaste.cs

[Line 227, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditHelperCopyPaste.cs#L227
), 
Michael Hoffmeister,
2020-08-01

    Operation mssing here?

[Line 249, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/DispEditHelperCopyPaste.cs#L249
), 
Michael Hoffmeister,
2020-08-01

    Operation mssing here?

## AasxWpfControlLibrary\PackageCentral\PackageCentral.cs

[Line 253, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageCentral.cs#L253
), 
MIHO,
2021-01-07

    rename to plural

## AasxWpfControlLibrary\PackageCentral\PackageConnectorHttpRest.cs

[Line 232, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageConnectorHttpRest.cs#L232
), 
all,
2021-01-30

    check periodically for supported element types

[Line 287, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageConnectorHttpRest.cs#L287
), 
MIHO,
2021-01-03

    check to handle more SMEs for AasEventMsgUpdateValue

[Line 288, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageConnectorHttpRest.cs#L288
), 
MIHO,
2021-01-04

    ValueIds still missing ..

## AasxWpfControlLibrary\PackageCentral\PackageContainerBase.cs

[Line 251, column 29](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerBase.cs#L251
), 
MIHO,
2021-01-03

    check to handle more SMEs for AasEventMsgUpdateValue

## AasxWpfControlLibrary\PackageCentral\PackageContainerBuffered.cs

[Line 66, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerBuffered.cs#L66
), 
MIHO,
2020-12-25

    think of creating a temp file which resemebles the source file

## AasxWpfControlLibrary\PackageCentral\PackageContainerFactory.cs

[Line 153, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerFactory.cs#L153
), 
MIHO,
2021-02-01

    check, if demo option is still required

## AasxWpfControlLibrary\PackageCentral\PackageContainerListBase.cs

[Line 318, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListBase.cs#L318
), 
MIHO,
2020-08-05

    refacture this with DispEditHelper.cs

## AasxWpfControlLibrary\PackageCentral\PackageContainerListHttpRestRepository.cs

[Line 103, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListHttpRestRepository.cs#L103
), 
MIHO,
2021-01-08

    check, how to make absolute

## AasxWpfControlLibrary\PackageCentral\PackageContainerListOfListControl.xaml.cs

[Line 121, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListOfListControl.xaml.cs#L121
), 
MIHO,
2021-01-09

    check to use moveup/down of the PackageContainerListBase

[Line 132, column 21](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerListOfListControl.xaml.cs#L132
), 
MIHO,
2021-01-09

    check to use moveup/down of the PackageContainerListBase

## AasxWpfControlLibrary\PackageCentral\PackageContainerLocalFile.cs

[Line 143, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerLocalFile.cs#L143
), 
MIHO,
2020-12-15

    consider removing "indirectLoadSave" from AdminShellPackageEnv

## AasxWpfControlLibrary\PackageCentral\PackageContainerRepoItem.cs

[Line 122, column 9](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/PackageCentral/PackageContainerRepoItem.cs#L122
), 
MIHO,
2021-01-08

    add SubmodelIds

## AasxWpfControlLibrary\VisualAasxElements.cs

[Line 152, column 17](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/AasxWpfControlLibrary/VisualAasxElements.cs#L152
), 
MIHO,
2020-07-31

    check if commented out because of non-working multi-select?

## WpfMtpControl\MtpAmlHelper.cs

[Line 51, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpAmlHelper.cs#L51
), 
MIHO,
2020-08-03

    see equivalent function in AmlImport.cs; may be re-use

[Line 219, column 41](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpAmlHelper.cs#L219
), 
MIHO,
2020-08-06

    spec/example files seem not to be in a final state

## WpfMtpControl\MtpVisuOpcUaClient.cs

[Line 242, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/MtpVisuOpcUaClient.cs#L242
), 
MIHO,
2020-08-06

    remove this, if not required anymore

## WpfMtpControl\UiElementHelper.cs

[Line 426, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/UiElementHelper.cs#L426
), 
MICHA,
2020-10-04

    check if font is set correctly ..

[Line 427, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpControl/UiElementHelper.cs#L427
), 
MICHA,
2020-10-04

    seems, that for Textblock the alignement DOES NOT WORK!

## WpfMtpVisuViewer\MainWindow.xaml.cs

[Line 76, column 13](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src/WpfMtpVisuViewer/MainWindow.xaml.cs#L76
), 
MIHO,
2020-09-18

    remove this test code


