using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Resources;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace AQI.AQILabs.Kernel.Numerics.Math.Properties
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0"),
    DebuggerNonUserCode,
    CompilerGenerated]
    internal class Resources
    {
        // Fields
        private static CultureInfo resourceCulture;
        private static ResourceManager resourceMan;

        // Methods
        internal Resources()
        {
        }

        // Properties
        internal static string ArrayParameterNotConformable
        {
            get
            {
                return ResourceManager.GetString("ArrayParameterNotConformable", resourceCulture);
            }
        }

        internal static string CalculateIncompleteGamma
        {
            get
            {
                return ResourceManager.GetString("CalculateIncompleteGamma", resourceCulture);
            }
        }

        internal static string CollectionEmpty
        {
            get
            {
                return ResourceManager.GetString("CollectionEmpty", resourceCulture);
            }
        }

        internal static string ComplexNotSupported
        {
            get
            {
                return ResourceManager.GetString("ComplexNotSupported", resourceCulture);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        internal static string EmptyOrNull
        {
            get
            {
                return ResourceManager.GetString("EmptyOrNull", resourceCulture);
            }
        }

        internal static string FileDoesNotExist
        {
            get
            {
                return ResourceManager.GetString("FileDoesNotExist", resourceCulture);
            }
        }

        internal static string InvalidQualifierCombination
        {
            get
            {
                return ResourceManager.GetString("InvalidQualifierCombination", resourceCulture);
            }
        }

        internal static string MinValueGreaterThanMaxValue
        {
            get
            {
                return ResourceManager.GetString("MinValueGreaterThanMaxValue", resourceCulture);
            }
        }

        internal static string MoreThan2D
        {
            get
            {
                return ResourceManager.GetString("MoreThan2D", resourceCulture);
            }
        }

        internal static string NameCannotContainASpace
        {
            get
            {
                return ResourceManager.GetString("NameCannotContainASpace", resourceCulture);
            }
        }

        internal static string NotMatrixMarketFile
        {
            get
            {
                return ResourceManager.GetString("NotMatrixMarketFile", resourceCulture);
            }
        }

        internal static string NotPositive
        {
            get
            {
                return ResourceManager.GetString("NotPositive", resourceCulture);
            }
        }

        internal static string NotProperHeader
        {
            get
            {
                return ResourceManager.GetString("NotProperHeader", resourceCulture);
            }
        }

        internal static string NotProperlyFormattedMatrixMarketFile
        {
            get
            {
                return ResourceManager.GetString("NotProperlyFormattedMatrixMarketFile", resourceCulture);
            }
        }

        internal static string NotSqurare
        {
            get
            {
                return ResourceManager.GetString("NotSqurare", resourceCulture);
            }
        }

        internal static string NullAction
        {
            get
            {
                return ResourceManager.GetString("NullAction", resourceCulture);
            }
        }

        internal static string NullParameterException
        {
            get
            {
                return ResourceManager.GetString("NullParameterException", resourceCulture);
            }
        }

        internal static string ParameterCannotBeNegative
        {
            get
            {
                return ResourceManager.GetString("ParameterCannotBeNegative", resourceCulture);
            }
        }

        internal static string ParameterNotConformable
        {
            get
            {
                return ResourceManager.GetString("ParameterNotConformable", resourceCulture);
            }
        }

        internal static string ParametersNotConformable
        {
            get
            {
                return ResourceManager.GetString("ParametersNotConformable", resourceCulture);
            }
        }

        internal static string ProposalDistributionNoUpperBound
        {
            get
            {
                return ResourceManager.GetString("ProposalDistributionNoUpperBound", resourceCulture);
            }
        }

        internal static string RandomNumberGeneratorCannotBeNull
        {
            get
            {
                return ResourceManager.GetString("RandomNumberGeneratorCannotBeNull", resourceCulture);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    ResourceManager manager = new ResourceManager("TimeSeries.Math.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = manager;
                }
                return resourceMan;
            }
        }

        internal static string ResultMatrixIncorrectDimensions
        {
            get
            {
                return ResourceManager.GetString("ResultMatrixIncorrectDimensions", resourceCulture);
            }
        }

        internal static string RowsLessThanColumns
        {
            get
            {
                return ResourceManager.GetString("RowsLessThanColumns", resourceCulture);
            }
        }

        internal static string SingularVectorsNotComputed
        {
            get
            {
                return ResourceManager.GetString("SingularVectorsNotComputed", resourceCulture);
            }
        }

        internal static string StandardDeviationCannotBeNegative
        {
            get
            {
                return ResourceManager.GetString("StandardDeviationCannotBeNegative", resourceCulture);
            }
        }

        internal static string UpperMustBeAtleastAsLargeAsLower
        {
            get
            {
                return ResourceManager.GetString("UpperMustBeAtleastAsLargeAsLower", resourceCulture);
            }
        }

        internal static string ZeroInfinityRange
        {
            get
            {
                return ResourceManager.GetString("ZeroInfinityRange", resourceCulture);
            }
        }

        internal static string ZeroOneRange
        {
            get
            {
                return ResourceManager.GetString("ZeroOneRange", resourceCulture);
            }
        }
    }
}