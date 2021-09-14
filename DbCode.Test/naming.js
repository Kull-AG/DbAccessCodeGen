function GetParameterTypeName(ident) {
    return new Identifier(settings.Namespace, def.MakeIdentifierValid(ident.Name) + "Parameters")
}
function GetResultTypeName(ident, type) {
    if (type == DBObjectType.View) {
        return new Identifier(settings.Namespace, def.MakeIdentifierValid(ident.Name));
    }
    return new Identifier(settings.Namespace, def.MakeIdentifierValid(ident.Name) + "Result")
}