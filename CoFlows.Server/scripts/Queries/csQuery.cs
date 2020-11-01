/// <info version="1.0.100">
///     <title>C# Query Test API</title>
///     <description>C# Query API with samples for permissions, documentation and function definitions</description>
///     <termsOfService url="https://www.coflo.ws"/>
///     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
///     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
/// </info>

public class XXX
{
    /// <api name="Add">
    ///     <description>Function that adds two numbers</description>
    ///     <returns>returns an integer</returns>
    ///     <param name="x" type="integer">First number to add</param>
    ///     <param name="y" type="integer">Second number to add</param>
    ///     <permissions>
    ///         <group id="$WID$" permission="read"/>
    ///     </permissions>
    /// </api>
    public static int Add(int x, int y)
    {
        return x + y;
    }
    
    /// <api name="Permission">
    ///     <description>Function that returns a permission</description>
    ///     <returns> returns an string</returns>
    ///     <permissions>
    ///         <group id="$WID$" permission="view"/>
    ///     </permissions>
    /// </api>
    public static string Permission()
    {
        var user = QuantApp.Kernel.User.ContextUser;
        var permission = QuantApp.Kernel.User.PermissionContext("$WID$");
        switch(permission)
        {
            case QuantApp.Kernel.AccessType.Write:
                return user.FirstName + " WRITE";
            case QuantApp.Kernel.AccessType.Read:
                return user.FirstName + " READ";
            case QuantApp.Kernel.AccessType.View:
                return user.FirstName + " VIEW";
            default:
                return user.FirstName + " DENIED";
        }
    }
}