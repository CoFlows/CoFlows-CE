Polyglot
===

In process interop in **CoFlows** is achieved by merging the functionality of various open-source projects. **CoreCLR** is the main execution environment that loads both the JVM and Python environments and also interprets Javascript. The Python environment is loaded through a fork of the [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet") library while the JVM is loaded through the [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM") libraries. Please note that the **PythonNet** fork was incorporated into the [QuantApp.Kernel/Python](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/Python "QAPy") package for additional integration.

## Languages
The **QuantApp Engine and Kernel** enable a polyglot environment where developers can code their functions in a variety of languages while allowing all functions irrespective of language to co-exist in the same process and memory. This mitigates the need for a TCP (SOAP, JSON WebAPI, Py4J) overhead which is usual when functions from different languages interact.

Within a **CoFlows** workflow, three computing environments interact within the same process including the **Core CLR** (DotNet Core Language Runtime), **JVM** (Java Virtual Machine) and the **Python interpreter**. Javascript is interpreted within the CLR using the Jint package. 

The full list is:
* CoreCLR: C#, F# & VB
* JVM: Java & Scala
* Python
* Javascript

## JVM <-> CoreCLR Link
As mentioned above, Python interop is achieved through [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet") so we will focus this conversation on the JVM <-> CLR link.

**QuantApp.Kernel/JVM** acts as a dynamic object broker that allows CLR (C#, F# and VB) code to create instances of JVM (Java and Scala) objects. Through reflection, the CLR Object mirrors of the JVM Object in terms of property and functions. When a property or a function is called in the CLR object, the **QuantApp.Kernel/JVM** library uses JNI to call the JVM Object's respective property or function and executes the JVM code in it's native environment. The result is then passed back to the CLR Object seamlessly for the user / developer. 

The same happens vice versa when JVM code calls native CLR objects. There are some subtle differences in user api between CLR code and JVM code. 

First of all, the CLR api (calling JVM from CLR) is quite similar in nature for all CLR languages. This is because C#, F# and VB are all first class citizens of the CLR environment and all have access to the same functionality in terms of **dynamic** objects and multi parameter delegates.

Although both Java and Scala live in the JVM world, Scala is more expressive than Java and to achieve this beauty, it needs to do a lot of magic on top of the JVM. To be clear, Scala only lives on the JVM, but to achieve it functionality, the bytecode compilation does many work arounds to compensate the lack of modern functionality that the JVM doesn't have. Therefor, the Scala api is much richer and syntactically pretty than the Java counterpart. For example, Scala has the concept of dynamic objects where properties and functions can be set at runtime, Java does not.

### Mapped Types and Lazy Execution

The **QuantApp.Kernel/JVM** library has mapped several "special" types between the VMs. When calling from the CLR, JVM Iterables for example are automatically mapped to IEnumerables in a lazy manner. This means that if CLR code is referencing a JVM List, an IEnumerable CLR object is created referencing to the JVM List of objects and you end up with only one List (JVM) and a reference to it.

Arrays on the other hand are copied and not referenced as in the example above. If a JVM Array is passed to the CLR, a new CLR array is created with either primitive values or JVM Objects (references) and you end up with two Arrays, one in CLR and the other in the JVM.

#### Mapped CLR Types
* Object <-> Object
* IEnumerable <-> Iterable
* IDictionary <-> Map
* Collection <-> Collection
* Tuple <-> Tuple (only Scala)
* Array <-> Array

#### Mapped JVM Types
* Object <-> Object
* Iterable <-> IEnumerable
* Array <-> Array

## JVM <-> Python
Since the main execution environment is CoreCLR, the link between the JVM and Python go through the CLR environment. Although there is more overhead between JVM and Python and between JVM and CLR, the communication still happens within the same process and memory so TCP or networking is not required.

These difference can be seen in the examples below.

## Notes
The Python link is achieved through the wonderful library [PythonNet](https://github.com/pythonnet/pythonnet "PythonNet").

The link between the **JVM** and **CLR** is achieved through the [QuantApp.Kernel/JVM](https://github.com/CoFlows/CoFlows-CE/tree/master/QuantApp.Kernel/JVM "QAJVM") libraries which have been developed from scratch for this project.

Javascript interpretation is achieved using the great [Jint](https://github.com/sebastienros/jint "Jint") library.
