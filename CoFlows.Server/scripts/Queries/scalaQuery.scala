/// <info version="1.0.100">
///     <title>Scala Query Test API</title>
///     <description>Scala Query API with samples for permissions, documentation and function definitions</description>
///     <termsOfService url="https://www.coflo.ws"/>
///     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
///     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
/// </info>

import scala.collection._
import app.quant.clr._
import app.quant.clr.scala.{SCLRObject => CLR}

import collection.JavaConverters._

class XXX {
    /// <api name="Add">
    ///     <description>Function that adds two numbers</description>
    ///     <returns>returns an integer</returns>
    ///     <param name="x" type="integer">First number to add</param>
    ///     <param name="y" type="integer">Second number to add</param>
    ///     <permissions>
    ///         <group id="$WID$" permission="read"/>
    ///     </permissions>
    /// </api>
    def Add(x:Int, y:Int) = x + y

    /// <api name="Permission">
    ///     <description>Function that returns a permission</description>
    ///     <returns> returns an string</returns>
    ///     <permissions>
    ///         <group id="$WID$" permission="view"/>
    ///     </permissions>
    /// </api>
    def Permission = {
    
        val userClass = CLR("QuantApp.Kernel.User")
        val user = userClass.GetContextUser[CLR]()
        val permission = userClass.PermissionContext[Int]("$WID$")
        
        permission match {        
            case 2 => user.FirstName[String] + " WRITE"
            case 1 => user.FirstName[String] + " READ"
            case 0 => user.FirstName[String] + " VIEW"
            case _ => user.FirstName[String] + " DENIED"
        }
    }
}