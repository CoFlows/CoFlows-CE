/// <info version="1.0.100">
///     <title>Javascript Query Test API</title>
///     <description>Javascript Query API with samples for permissions, documentation and function definitions</description>
///     <termsOfService url="https://www.coflo.ws"/>
///     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
///     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
/// </info>

var qkernel = importNamespace('QuantApp.Kernel')

/// <api name="Add">
///     <description>Function that adds two numbers</description>
///     <returns>returns an integer</returns>
///     <param name="x" type="integer">First number to add</param>
///     <param name="y" type="integer">Second number to add</param>
///     <permissions>
///         <group id="$WID$" permission="read"/>
///     </permissions>
/// </api>
let Add = function(x, y) {
        return x + y
    }

/// <api name="Permission">
///     <description>Function that returns a permission</description>
///     <returns> returns an string</returns>
///     <permissions>
///         <group id="$WID$" permission="view"/>
///     </permissions>
/// </api>
let Permission = function() {
    var user = qkernel.User.ContextUser
    var permission = qkernel.User.PermissionContext("$WID$")
    switch(permission)
    {
        case qkernel.AccessType.Write:
            return user.FirstName + " WRITE"
        case qkernel.AccessType.Read:
            return user.FirstName + " READ"
        case qkernel.AccessType.View:
            return user.FirstName + " VIEW"
        default:
            return user.FirstName + " DENIED"
    }
}