// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

// Extension typing, validation of extension types, etc.

module rec FSharp.Compiler.TypeProviders

#if !NO_TYPEPROVIDERS

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Reflection
open Internal.Utilities.Library
open FSharp.Core.CompilerServices
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.Text

type TypeProviderDesignation = TypeProviderDesignation of string

/// Raised when a type provider has thrown an exception.
exception ProvidedTypeResolution of range * exn

/// Raised when an type provider has thrown an exception.
exception ProvidedTypeResolutionNoRange of exn

/// Get the list of relative paths searched for type provider design-time components
val toolingCompatiblePaths: unit -> string list

/// Carries information about the type provider resolution environment.
type ResolutionEnvironment =
    {
        /// The folder from which an extension provider is resolving from. This is typically the project folder.
        ResolutionFolder: string

        /// Output file name
        OutputFile: string option

        /// Whether or not the --showextensionresolution flag was supplied to the compiler.
        ShowResolutionMessages: bool

        /// All referenced assemblies, including the type provider itself, and possibly other type providers.
        GetReferencedAssemblies: unit -> string[]

        /// The folder for temporary files
        TemporaryFolder: string
    }

/// Find and instantiate the set of ITypeProvider components for the given assembly reference
val GetTypeProvidersOfAssembly:
    runtimeAssemblyFilename: string *
    ilScopeRefOfRuntimeAssembly: ILScopeRef *
    designTimeName: string *
    resolutionEnvironment: ResolutionEnvironment *
    isInvalidationSupported: bool *
    isInteractive: bool *
    systemRuntimeContainsType: (string -> bool) *
    systemRuntimeAssemblyVersion: Version *
    compilerToolPaths: string list *
    m: range ->
        Tainted<ITypeProvider> list

/// Given an extension type resolver, supply a human-readable name suitable for error messages.
val DisplayNameOfTypeProvider: Tainted<ITypeProvider> * range -> string

/// The context used to interpret information in the closure of System.Type, System.MethodInfo and other
/// info objects coming from the type provider.
///
/// At the moment this is the "Type --> ILTypeRef" and "Type --> Tycon" remapping
/// context for generated types (it is empty for erased types). This is computed from
/// while processing the [<Generate>] declaration related to the type.
///
/// Immutable (after type generation for a [<Generate>] declaration populates the dictionaries).
///
/// The 'obj' values are all TyconRef, but obj is used due to a forward reference being required. Not particularly
/// pleasant, but better than intertwining the whole "ProvidedType" with the TAST structure.
[<Sealed>]
type ProvidedTypeContext =

    member TryGetILTypeRef: ProvidedType -> ILTypeRef option

    member TryGetTyconRef: ProvidedType -> obj option

    static member Empty: ProvidedTypeContext

    static member Create:
        ConcurrentDictionary<ProvidedType, ILTypeRef> * ConcurrentDictionary<ProvidedType, obj (* TyconRef *) > ->
            ProvidedTypeContext

    member GetDictionaries:
        unit -> ConcurrentDictionary<ProvidedType, ILTypeRef> * ConcurrentDictionary<ProvidedType, obj (* TyconRef *) >

    /// Map the TyconRef objects, if any
    member RemapTyconRefs: (obj -> obj) -> ProvidedTypeContext

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedType =
    new: x: System.Type * ctxt: ProvidedTypeContext -> ProvidedType
    inherit ProvidedMemberInfo

    abstract member IsSuppressRelocate: bool

    abstract member IsErased: bool

    abstract member IsGenericType: bool

    abstract member Namespace: string

    abstract member FullName: string

    abstract member IsArray: bool

    abstract member GetInterfaces: unit -> ProvidedType[]

    abstract member Assembly: ProvidedAssembly

    abstract member BaseType: ProvidedType MaybeNull

    abstract member GetNestedType: string -> ProvidedType

    abstract member GetNestedTypes: unit -> ProvidedType[]

    abstract member GetAllNestedTypes: unit -> ProvidedType[]

    abstract member GetMethods: unit -> ProvidedMethodInfo[]

    abstract member GetFields: unit -> ProvidedFieldInfo[]

    abstract member GetField: string -> ProvidedFieldInfo

    abstract member GetProperties: unit -> ProvidedPropertyInfo[]

    abstract member GetProperty: string -> ProvidedPropertyInfo

    abstract member GetEvents: unit -> ProvidedEventInfo[]

    abstract member GetEvent: string -> ProvidedEventInfo

    abstract member GetConstructors: unit -> ProvidedConstructorInfo[]

    abstract member GetStaticParameters: ITypeProvider -> ProvidedParameterInfo[]

    abstract member GetGenericTypeDefinition: unit -> ProvidedType

    abstract member IsVoid: bool

    abstract member IsGenericParameter: bool

    abstract member IsValueType: bool

    abstract member IsByRef: bool

    abstract member IsPointer: bool

    abstract member IsEnum: bool

    abstract member IsInterface: bool

    abstract member IsClass: bool

    abstract member IsMeasure: bool

    abstract member IsSealed: bool

    abstract member IsAbstract: bool

    abstract member IsPublic: bool

    abstract member IsNestedPublic: bool

    abstract member GenericParameterPosition: int

    abstract member GetElementType: unit -> ProvidedType

    abstract member GetGenericArguments: unit -> ProvidedType[]

    abstract member GetArrayRank: unit -> int

    member RawSystemType: Type

    abstract member GetEnumUnderlyingType: unit -> ProvidedType

    abstract member MakePointerType: unit -> ProvidedType

    abstract member MakeByRefType: unit -> ProvidedType

    abstract member MakeArrayType: unit -> ProvidedType

    abstract member MakeArrayType: rank: int -> ProvidedType

    abstract member MakeGenericType: args: ProvidedType[] -> ProvidedType

    abstract member AsProvidedVar: name: string -> ProvidedVar

    static member Void: ProvidedType

    static member CreateNoContext: Type -> ProvidedType

    member TryGetILTypeRef: unit -> ILTypeRef option

    member TryGetTyconRef: unit -> obj option

    abstract member ApplyContext: ProvidedTypeContext -> ProvidedType

    abstract member Context: ProvidedTypeContext

    static member TaintedEquals: Tainted<ProvidedType> * Tainted<ProvidedType> -> bool

    abstract member ApplyStaticArguments: ITypeProvider * string [] * obj [] -> ProvidedType

#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type IProvidedCustomAttributeProvider =
    abstract GetCustomAttributes: provider: ITypeProvider -> seq<CustomAttributeData>

    abstract GetHasTypeProviderEditorHideMethodsAttribute: provider: ITypeProvider -> bool

    abstract GetDefinitionLocationAttribute: provider: ITypeProvider -> (string MaybeNull * int * int) option

    abstract GetXmlDocAttributes: provider: ITypeProvider -> string[]

    abstract GetAttributeConstructorArgs:
        provider: ITypeProvider * attribName: string -> (obj option list * (string * obj option) list) option
    
[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedAssembly =
    new: x: System.Reflection.Assembly -> ProvidedAssembly

    abstract member GetName: unit -> System.Reflection.AssemblyName

    abstract member FullName: string

    abstract member GetManifestModuleContents: ITypeProvider -> byte[]

    member Handle: System.Reflection.Assembly

[<AbstractClass>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedMemberInfo = 

    abstract member Name: string

    abstract member DeclaringType: ProvidedType MaybeNull

    abstract GetCustomAttributes : provider: ITypeProvider -> seq<CustomAttributeData>

    abstract GetHasTypeProviderEditorHideMethodsAttribute : provider:ITypeProvider -> bool

    abstract GetDefinitionLocationAttribute : provider:ITypeProvider -> (string * int * int) option 

    abstract GetXmlDocAttributes : provider:ITypeProvider -> string[]

    abstract GetAttributeConstructorArgs: provider:ITypeProvider * attribName:string -> (obj option list * (string * obj option) list) option

    interface IProvidedCustomAttributeProvider 

[<AbstractClass>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedMethodBase = 
    inherit ProvidedMemberInfo

    member Context: ProvidedTypeContext

    abstract member IsGenericMethod: bool

    abstract member IsStatic: bool

    abstract member IsFamily: bool

    abstract member IsFamilyAndAssembly: bool

    abstract member IsFamilyOrAssembly: bool

    abstract member IsVirtual: bool

    abstract member IsFinal: bool

    abstract member IsPublic: bool

    abstract member IsAbstract: bool

    abstract member IsHideBySig: bool

    abstract member IsConstructor: bool

    abstract member GetParameters: unit -> ProvidedParameterInfo[]

    abstract member GetGenericArguments: unit -> ProvidedType[]

    abstract member GetStaticParametersForMethod: ITypeProvider -> ProvidedParameterInfo []

    abstract member ApplyStaticArgumentsForMethod:
        provider: ITypeProvider * fullNameAfterArguments: string * staticArgs: obj [] -> ProvidedMethodBase

    static member TaintedGetHashCode: Tainted<ProvidedMethodBase> -> int

    static member TaintedEquals: Tainted<ProvidedMethodBase> * Tainted<ProvidedMethodBase> -> bool

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedMethodInfo =
    new: x: System.Reflection.MethodInfo * ctxt: ProvidedTypeContext -> ProvidedMethodInfo

    inherit ProvidedMethodBase

    abstract member ReturnType: ProvidedType

    abstract member MetadataToken: int

    member Handle: System.Reflection.MethodInfo

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedParameterInfo =
    new: x: System.Reflection.ParameterInfo * ctxt: ProvidedTypeContext -> ProvidedParameterInfo

    abstract member Name: string

    abstract member ParameterType: ProvidedType

    abstract member IsIn: bool

    abstract member IsOut: bool

    abstract member IsOptional: bool

    abstract member RawDefaultValue: objnull

    abstract member HasDefaultValue: bool

    abstract GetCustomAttributes : provider: ITypeProvider -> seq<CustomAttributeData>

    abstract GetHasTypeProviderEditorHideMethodsAttribute : provider:ITypeProvider -> bool

    abstract GetDefinitionLocationAttribute : provider:ITypeProvider -> (string * int * int) option

    abstract GetXmlDocAttributes : provider:ITypeProvider -> string[]

    abstract GetAttributeConstructorArgs: provider:ITypeProvider * attribName:string -> (obj option list * (string * obj option) list) option

    interface IProvidedCustomAttributeProvider

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedFieldInfo =
    inherit ProvidedMemberInfo

    new: x: System.Reflection.FieldInfo * ctxt: ProvidedTypeContext -> ProvidedFieldInfo

    abstract member IsInitOnly: bool

    abstract member IsStatic: bool

    abstract member IsSpecialName: bool

    abstract member IsLiteral: bool

    abstract member GetRawConstantValue: unit -> obj

    abstract member FieldType: ProvidedType

    abstract member IsPublic: bool

    abstract member IsFamily: bool

    abstract member IsFamilyAndAssembly: bool

    abstract member IsFamilyOrAssembly: bool

    abstract member IsPrivate: bool

    static member TaintedEquals: Tainted<ProvidedFieldInfo> * Tainted<ProvidedFieldInfo> -> bool

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedPropertyInfo =
    inherit ProvidedMemberInfo

    new: x: System.Reflection.PropertyInfo * ctxt: ProvidedTypeContext -> ProvidedPropertyInfo

    abstract member GetGetMethod: unit -> ProvidedMethodInfo

    abstract member GetSetMethod: unit -> ProvidedMethodInfo

    abstract member GetIndexParameters: unit -> ProvidedParameterInfo[]

    abstract member CanRead: bool

    abstract member CanWrite: bool

    abstract member PropertyType: ProvidedType

    static member TaintedGetHashCode: Tainted<ProvidedPropertyInfo> -> int

    static member TaintedEquals: Tainted<ProvidedPropertyInfo> * Tainted<ProvidedPropertyInfo> -> bool

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedEventInfo =
    inherit ProvidedMemberInfo

    new: x: System.Reflection.EventInfo * ctxt: ProvidedTypeContext -> ProvidedEventInfo

    abstract member GetAddMethod: unit -> ProvidedMethodInfo

    abstract member GetRemoveMethod: unit -> ProvidedMethodInfo

    abstract member EventHandlerType: ProvidedType

    static member TaintedGetHashCode: Tainted<ProvidedEventInfo> -> int

    static member TaintedEquals: Tainted<ProvidedEventInfo> * Tainted<ProvidedEventInfo> -> bool

[<Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedConstructorInfo =
    inherit ProvidedMethodBase

    new: x: System.Reflection.ConstructorInfo * ctxt: ProvidedTypeContext -> ProvidedConstructorInfo

type ProvidedExprType =

    | ProvidedNewArrayExpr of ProvidedType * ProvidedExpr[]

    | ProvidedNewObjectExpr of ProvidedConstructorInfo * ProvidedExpr[]

    | ProvidedWhileLoopExpr of ProvidedExpr * ProvidedExpr

    | ProvidedNewDelegateExpr of ProvidedType * ProvidedVar[] * ProvidedExpr

    | ProvidedForIntegerRangeLoopExpr of ProvidedVar * ProvidedExpr * ProvidedExpr * ProvidedExpr

    | ProvidedSequentialExpr of ProvidedExpr * ProvidedExpr

    | ProvidedTryWithExpr of ProvidedExpr * ProvidedVar * ProvidedExpr * ProvidedVar * ProvidedExpr

    | ProvidedTryFinallyExpr of ProvidedExpr * ProvidedExpr

    | ProvidedLambdaExpr of ProvidedVar * ProvidedExpr

    | ProvidedCallExpr of ProvidedExpr option * ProvidedMethodInfo * ProvidedExpr[]

    | ProvidedConstantExpr of obj * ProvidedType

    | ProvidedDefaultExpr of ProvidedType

    | ProvidedNewTupleExpr of ProvidedExpr[]

    | ProvidedTupleGetExpr of ProvidedExpr * int

    | ProvidedTypeAsExpr of ProvidedExpr * ProvidedType

    | ProvidedTypeTestExpr of ProvidedExpr * ProvidedType

    | ProvidedLetExpr of ProvidedVar * ProvidedExpr * ProvidedExpr

    | ProvidedVarSetExpr of ProvidedVar * ProvidedExpr

    | ProvidedIfThenElseExpr of ProvidedExpr * ProvidedExpr * ProvidedExpr

    | ProvidedVarExpr of ProvidedVar
    
[<RequireQualifiedAccess; Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedExpr =
    new: x: Quotations.Expr * ctxt: ProvidedTypeContext -> ProvidedExpr

    abstract member Type: ProvidedType

    /// Convert the expression to a string for diagnostics
    abstract member UnderlyingExpressionString: string

    abstract member GetExprType: unit -> ProvidedExprType option

    member Handle: Quotations.Expr

[<RequireQualifiedAccess; Class>] 
#if NO_CHECKNULLS
[<AllowNullLiteral>]
#endif
type ProvidedVar =
    new: x: Quotations.Var * ctxt: ProvidedTypeContext -> ProvidedVar

    abstract member Type: ProvidedType

    abstract member Name: string

    abstract member IsMutable: bool

    override Equals: obj -> bool

    override GetHashCode: unit -> int

/// Get the provided expression for a particular use of a method.
val GetInvokerExpression: ITypeProvider * ProvidedMethodBase * ProvidedVar[] -> ProvidedExpr

/// Get all provided types from provided namespace
val GetProvidedTypes: pn: IProvidedNamespace -> ProvidedType []

/// Validate that the given provided type meets some of the rules for F# provided types
val ValidateProvidedTypeAfterStaticInstantiation:
    m: range * st: Tainted<ProvidedType> * expectedPath: string[] * expectedName: string -> unit

/// Try to apply a provided type to the given static arguments. If successful also return a function
/// to check the type name is as expected (this function is called by the caller of TryApplyProvidedType
/// after other checks are made).
val TryApplyProvidedType:
    typeBeforeArguments: Tainted<ProvidedType> * optGeneratedTypePath: string list option * staticArgs: objnull[] * range ->
        (Tainted<ProvidedType> * (unit -> unit)) option

/// Try to apply a provided method to the given static arguments.
val TryApplyProvidedMethod:
    methBeforeArgs: Tainted<ProvidedMethodBase> * staticArgs: objnull[] * range -> Tainted<ProvidedMethodBase> option

/// Try to resolve a type in the given extension type resolver
val TryResolveProvidedType: Tainted<ITypeProvider> * range * string[] * typeName: string -> Tainted<ProvidedType> option

/// Try to resolve a type in the given extension type resolver
val TryLinkProvidedType:
    Tainted<ITypeProvider> * string[] * typeLogicalName: string * range: range -> Tainted<ProvidedType> option

/// Get the parts of a .NET namespace. Special rules: null means global, empty is not allowed.
val GetProvidedNamespaceAsPath: range * Tainted<ITypeProvider> * string -> string list

/// Decompose the enclosing name of a type (including any class nestings) into a list of parts.
/// e.g. System.Object -> ["System"; "Object"]
val GetFSharpPathToProvidedType: Tainted<ProvidedType> * range: range -> string list

/// Get the ILTypeRef for the provided type (including for nested types). Take into account
/// any type relocations or static linking for generated types.
val GetILTypeRefOfProvidedType: Tainted<ProvidedType> * range: range -> ILTypeRef

/// Get the ILTypeRef for the provided type (including for nested types). Do not take into account
/// any type relocations or static linking for generated types.
val GetOriginalILTypeRefOfProvidedType: Tainted<ProvidedType> * range: range -> ILTypeRef

/// Represents the remapping information for a generated provided type and its nested types.
///
/// There is one overall tree for each root 'type X = ... type generation expr...' specification.
type ProviderGeneratedType =
    | ProviderGeneratedType of ilOrigTyRef: ILTypeRef * ilRenamedTyRef: ILTypeRef * ProviderGeneratedType list

/// The table of information recording remappings from type names in the provided assembly to type
/// names in the statically linked, embedded assembly, plus what types are nested in side what types.
type ProvidedAssemblyStaticLinkingMap =
    {
        /// The table of remappings from type names in the provided assembly to type
        /// names in the statically linked, embedded assembly.
        ILTypeMap: Dictionary<ILTypeRef, ILTypeRef>
    }

    /// Create a new static linking map, ready to populate with data.
    static member CreateNew: unit -> ProvidedAssemblyStaticLinkingMap

/// Check if this is a direct reference to a non-embedded generated type. This is not permitted at any name resolution.
/// We check by seeing if the type is absent from the remapping context.
val IsGeneratedTypeDirectReference: Tainted<ProvidedType> * range -> bool

[<AutoOpen>]
module Shim =

    type IExtensionTypingProvider =

        /// Find and instantiate the set of ITypeProvider components for the given assembly reference
        abstract InstantiateTypeProvidersOfAssembly:
            runtimeAssemblyFilename: string *
            designerAssemblyName: string *
            ResolutionEnvironment *
            bool *
            isInteractive: bool *
            systemRuntimeContainsType: (string -> bool) *
            systemRuntimeAssemblyVersion: System.Version *
            compilerToolsPath: string list *
            logError: (TypeProviderError -> unit) *
            m: range ->
                ITypeProvider list

        abstract GetProvidedTypes: pn: IProvidedNamespace -> ProvidedType []
        abstract ResolveTypeName: pn: IProvidedNamespace * typeName: string -> ProvidedType
        abstract GetInvokerExpression:
            provider: ITypeProvider * methodBase: ProvidedMethodBase * paramExprs: ProvidedVar [] -> ProvidedExpr
        abstract DisplayNameOfTypeProvider: typeProvider: ITypeProvider * fullName: bool -> string

    [<Sealed>]
    type DefaultExtensionTypingProvider =
        interface IExtensionTypingProvider

    [<Sealed>]
    type ExtensionTyping =
        static member Provider: IExtensionTypingProvider with get, set

#endif
