namespace AzureADB2CApi.Helpers
{
    internal class B2CCustomAttributeHelper
    {
        internal readonly string B2CExtensionAppClientId;

        internal B2CCustomAttributeHelper(string b2CExtensionAppClientId)
        {
            B2CExtensionAppClientId = b2CExtensionAppClientId.Replace("-", "");
        }

        internal string GetCompleteAttributeName(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new System.ArgumentException("Parameter cannot be null", nameof(attributeName));
            }

            return $"extension_{B2CExtensionAppClientId}_{attributeName}";
        }
    }
}
