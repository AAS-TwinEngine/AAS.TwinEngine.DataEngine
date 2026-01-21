namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;

public static class ExceptionMessages
{
    public const string InvalidRequestedLimit = "Limit must be greater than or equal to 1.";
    public const string InvalidRequestPayload = "The request payload is invalid.";
    public const string RequestBodyInvalid = "The request body could not be processed. Verify the structure and try again.";
    public const string FailedParsingJsonSchema = "Failed to parse JSON schema.";
    public const string InvalidJsonSchemaRootElement = "The JSON schema must contain a root element.";
    public const string UnknownTypeError = "Unknown node type.";
    public const string ResourceNotFound = "Required resource not found";
    public const string ResourceNotValid = "Required resource is not valid";
    public const string ResponseIsNotValidate = "Response body is not valid";
    public const string ShellDescriptorDataNotFound = "Required shell information not found";
    public const string AssetNotFound = "Required asset information not found";
}
