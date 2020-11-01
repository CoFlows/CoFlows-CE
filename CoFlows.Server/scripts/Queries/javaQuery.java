/// <info version="1.0.100">
///     <title>Java Query Test API</title>
///     <description>Java Query API with samples for permissions, documentation and function definitions</description>
///     <termsOfService url="https://www.coflo.ws"/>
///     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
///     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
/// </info>

import app.quant.clr.*;
import java.util.*;

class XXX
{
    public XXX(){}

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
    public static String Permission()
    {
        CLRObject userClass = CLRRuntime.GetClass("QuantApp.Kernel.User");
        CLRObject user = (CLRObject)userClass.Invoke("GetContextUser");
        int permission = (int)userClass.Invoke("PermissionContext", "$WID$");
        
        switch(permission)
        {
            case 2:
                return user.GetProperty("FirstName") + " WRITE";
            case 1:
                return user.GetProperty("FirstName") + " READ";
            case 0:
                return user.GetProperty("FirstName") + " VIEW";
            default:
                return user.GetProperty("FirstName") + " DENIED";
        }
    }
}