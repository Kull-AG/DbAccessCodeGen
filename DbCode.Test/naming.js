function GetParameterTypeName(ident) {
    return new Identifier(settings.Namespace, def.MakeIdentifierValid(ident.Name) + "Parameters")
}
function GetResultTypeName(ident) {
    return new Identifier(settings.Namespace, def.MakeIdentifierValid(ident.Name) + "Result")
}