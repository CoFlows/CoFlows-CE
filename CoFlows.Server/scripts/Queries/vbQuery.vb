''' <info version="1.0.100">
'''     <title>C# Query Test API</title>
'''     <description>C# Query API with samples for permissions, documentation and function definitions</description>
'''     <termsOfService url="https://www.coflo.ws"/>
'''     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
'''     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
''' </info>

Public Class XXX    
    ''' <api name="Add">
    '''     <description>Function that adds two numbers</description>
    '''     <returns>returns an integer</returns>
    '''     <param name="x" type="integer">First number to add</param>
    '''     <param name="y" type="integer">Second number to add</param>
    '''     <permissions>
    '''         <group id="$WID$" permission="read"/>
    '''     </permissions>
    ''' </api>
    Public Shared Function Add(x as Integer, y as Integer) As Integer
        Return x + y
    End Function
End Class

