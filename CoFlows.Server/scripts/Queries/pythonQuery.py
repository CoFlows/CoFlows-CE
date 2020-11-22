### <info version="1.0.100">
###     <title>Python Query Test API</title>
###     <description>Python Query API with samples for permissions, documentation and function definitions</description>
###     <termsOfService url="https://www.coflo.ws"/>
###     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
###     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
### </info>

import QuantApp.Kernel as qak

### <api name="Add">
###     <description>Function that adds two numbers</description>
###     <returns>returns an integer</returns>
###     <param name="x" type="integer">First number to add</param>
###     <param name="y" type="integer">Second number to add</param>
###     <permissions>
###         <group id="$WID$" permission="read"/>
###     </permissions>
### </api>
def Add(x, y):
    return int(x) + int(y)


### <api name="Permission">
###     <description>Function that returns a permission</description>
###     <returns> returns an string</returns>
###     <permissions>
###         <group id="$WID$" permission="view"/>
###     </permissions>
### </api>
def Permission():
    quser = qak.User.ContextUser
    permission = qak.User.PermissionContext("$WID$")
    if permission == qak.AccessType.Write:
        return quser.FirstName + " WRITE"
    elif permission == qak.AccessType.Read:
        return quser.FirstName + " READ"
    elif permission == qak.AccessType.View:
        return quser.FirstName + " VIEW"
    else:
        return quser.FirstName + " DENIED"