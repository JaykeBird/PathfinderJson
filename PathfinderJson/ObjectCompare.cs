using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PathfinderJson
{
    /// <summary>
    /// A static class with functions to compare if two objects are equal.
    /// </summary>
    public static class Compare
    {

        /// <summary>
        /// Compares the properties and fields of two objects, and checks if the two are equal.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="a">The first object</param>
        /// <param name="b">The second object</param>
        /// <returns>A <see cref="CompareResult"/> object with results of the comparison, including a list of inequal properties/fields</returns>
        /// <remarks>
        /// This uses reflection to compare the two objects, by sequentially checking and comparing each property and field.
        /// Note that this is made for comparing two objects that have properties. This will not work with
        /// many value types, enums, and IEnumerable objects - inputting those will return a
        /// <see cref="CompareResult"/> object with the result of <see cref="CompareSuccessValue.NotSupported"/>.<para/>
        /// Indexer properties (i.e. properties like <c>this[int index]</c>) will not be compared.
        /// Properties with the <see cref="DoNotCompareAttribute"/> attribute will also not be compared.<para/>
        /// If comparing two objects and one or both are <c>null</c>, the resulting <see cref="CompareResult"/> object will have a result of
        /// <see cref="CompareSuccessValue.NullObject"/>, and will indicate both are equal if both are null.<para/>
        /// Only public properties and fields are checked; to also check private properties or fields, or to also leverage the usage of
        /// <see cref="IEqualityComparer"/> objects to help speed up comparison, please use <see cref="CompareObjects{T}(T, T, IEqualityComparer[], bool)"/>.
        /// </remarks>
        public static CompareResult CompareObjects<T>(T a, T b)
        {
            return CompareObjects(a, b, Array.Empty<IEqualityComparer>(), false);
        }

        /// <summary>
        /// Compares the properties and values of two objects, and checks if the two are equal.
        /// </summary>
        /// <typeparam name="T">The type of the objects to compare.</typeparam>
        /// <param name="a">The first object</param>
        /// <param name="b">The second object</param>
        /// <param name="equalityComparers">a collection of equality comparer objects to use for comparing two objects of various types</param>
        /// <param name="includePrivateValues">whether private properties, fields, and others will be compared</param>
        /// <returns>A <see cref="CompareResult"/> object with results of the comparison, including a list of inequal properties/values</returns>
        /// <remarks>
        /// This uses reflection to compare the two objects, by sequentially checking and comparing each property and field.
        /// Note that this is made for comparing two objects that have properties. This will not work with enums and IEnumerable objects
        /// - inputting those will return a <see cref="CompareResult"/> object with the result <see cref="CompareSuccessValue.NotSupported"/>.<para/>
        /// Indexer properties (i.e. properties like <c>this[int index]</c>) will not be compared.
        /// Properties with the <see cref="DoNotCompareAttribute"/> attribute will also not be compared.<para/>
        /// If comparing two objects and one or both are <c>null</c>, the resulting <see cref="CompareResult"/> object will have a result
        /// <see cref="CompareSuccessValue.NullObject"/>, and will indicate both are equal if both are null.
        /// </remarks>
        public static CompareResult CompareObjects<T>(T a, T b, IEqualityComparer[] equalityComparers, bool includePrivateValues = false)
        {
            Console.WriteLine($"Comparing {typeof(T).Name}");
            if (a is IEquatable<T> ea) // if A is IEquatable, then so is B
            {
                Console.WriteLine("> IEquatable");
                if (ea.Equals(b)) // just a quick check to avoid comparing when not needed - if both are equal, then we are done here
                {
                    return new CompareResult(true);
                }

                // many of the types that I was worried about not being able to cover actually implements IEquatable...
                // this includes strings and primitive value types (i.e. int, bool, double), so those are technically covered here
                // notably, though, enums are not covered in this, and so will fail out later
            }

            // check for null
            if (a == null && b == null)
            {
                return new CompareResult(CompareSuccessValue.NullObject, true);
            }
            else if (a == null || b == null)
            {
                return new CompareResult(CompareSuccessValue.NullObject);
            }

            // check if it is a string
            if (a is string)
            {
                return new CompareResult((a as string) == (b as string));
            }
            else if (a is IEnumerable)
            {
                return new CompareResult(CompareSuccessValue.NotSupported);
            }

            // okay, let's open up the type
            Type t = typeof(T);
            if (t.IsPrimitive || t.IsEnum) // quick check for other invalid types that I can't work with
            {
                return new CompareResult(CompareSuccessValue.NotSupported);
            }

            // let's store a few things that I'll refer back to later
            Type iEquatable = typeof(IEquatable<>);
            List<string> differingProperties = new List<string>();

            // now, let's look at each property
            BindingFlags propertyFlags = BindingFlags.Instance | BindingFlags.Public;
            if (includePrivateValues) propertyFlags |= BindingFlags.NonPublic;

            // now, we'll dive into each property to check on their types and start comparing values
            foreach (PropertyInfo prop in t.GetProperties(propertyFlags))
            {
                Console.WriteLine("> Comparing " + prop.Name);
                // if a property has a DoNotCompareAttribute, we'll skip it
                if (prop.CustomAttributes.Any(a => a.AttributeType == typeof(DoNotCompareAttribute)))
                {
                    continue;
                }

                ParameterInfo[] indexers = prop.GetIndexParameters();
                if (indexers.Length != 0)
                {
                    // this is an indexer property
                    // we'll skip this, since it'd be impossible to check every possible indexer value
                    continue;
                }

                Type propType = prop.PropertyType;
                dynamic? objA = prop.GetValue(a);
                dynamic? objB = prop.GetValue(b);

                if (objA is null && objB is null)
                {
                    Console.WriteLine(">   > null and null");
                    // both are null, continue
                    continue;
                }
                else if (objA is null || objB is null)
                {
                    Console.WriteLine(">   > null and... not null");
                    // one is null, one is not
                    differingProperties.Add(prop.Name);
                    continue;
                }

                // let's see if this type is an IEquatable type
                Type iEquatableType = iEquatable.MakeGenericType(propType);
                if (iEquatableType.IsAssignableFrom(propType))
                {
                    Console.WriteLine(">   > IEquatable property");
                    // this is an IEquatable<T>
                    if (!objA.Equals(objB))
                    {
                        Console.WriteLine(">   >   > Differs");
                        differingProperties.Add(prop.Name);
                    }
                    else Console.WriteLine(">   >   > Same");
                    continue;
                }
                if (propType.IsEnum)
                {
                    Console.WriteLine(">   > Enum property");
                    // this is an enum, let's just dynamically compare the two
                    if (objA != objB)
                    {
                        Console.WriteLine(">   >   > Differs");
                        differingProperties.Add(prop.Name);
                    }
                    else Console.WriteLine(">   >   > Same");
                    continue;
                }
                else if (typeof(IEnumerable).IsAssignableFrom(propType))
                {
                    Console.WriteLine(">   > IEnumerable property");
                    // this is an IEnumerable
                    if (!CompareEnumerables(objA, objB))
                    {
                        Console.WriteLine(">   >   > Differs");
                        differingProperties.Add(prop.Name);
                    }
                    else Console.WriteLine(">   >   > Same");
                    continue;
                }
                else
                {
                    Console.WriteLine(">   > Further compare");
                    // this is a different kind of object... time to go down the rabbit hole!
                    // just doing a simple CompareObjects with the dynamic types doesn't work
                    // ... it attempts to compare them as Object class items, rather than getting specific
                    // so instead let's use reflection to call this current function
                    MethodInfo mi = typeof(Compare).GetMethods().Where(m => m.Name == nameof(CompareObjects))
                        .Where(m => m.GetParameters().Length == 2).First();
                    mi = mi.MakeGenericMethod(prop.PropertyType);
                    object? result = mi.Invoke(null, new object?[] { objA, objB });
                    if (result is CompareResult cr && cr.Equal == false)
                    {
                        differingProperties.Add(prop.Name);
                    }
                    Console.WriteLine("Back to parent compare");
                }
            }

            return new CompareResult(differingProperties);
        }

        /// <summary>
        /// Compares two <see cref="IEnumerable"/> objects, and checks if both are equal (as in, having equal values).
        /// </summary>
        /// <typeparam name="T">The type of the objects contained within the IEnumerable objects.</typeparam>
        /// <param name="a">the first object to compare</param>
        /// <param name="b">the second object to compare</param>
        /// <param name="matchSequence">
        /// compare both the items and the sequence of the items in the enumerables; if <c>false</c>, the sequence can differ
        /// </param>
        /// <returns><c>true</c> if equal, <c>false</c> otherwise.</returns>
        public static bool CompareEnumerables<T>(IEnumerable<T> a, IEnumerable<T> b, bool matchSequence = true)
        {
            return CompareEnumerables(a, b, EqualityComparer<T>.Default, matchSequence);
        }

        /// <summary>
        /// Compares two <see cref="IEnumerable"/> objects, and checks if both are equal (as in, having equal values).
        /// </summary>
        /// <typeparam name="T">The type of the objects contained within the IEnumerable objects.</typeparam>
        /// <param name="a">the first object to compare</param>
        /// <param name="b">the second object to compare</param>
        /// <param name="comparer">The comparer to use to compare values within the two objects.</param>
        /// <param name="matchSequence">
        /// compare both the items and the sequence of the items in the enumerables; if <c>false</c>, the sequence can differ,
        /// but both enumerables must have the same items
        /// </param>
        /// <returns><c>true</c> if equal, <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Note that this will necessarily include enumerating both <see cref="IEnumerable"/> objects; for objects that are
        /// a list, collection, or dictionary, this will not pose any problem.
        /// </remarks>
        public static bool CompareEnumerables<T>(IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> comparer, bool matchSequence = true)
        {
            Console.WriteLine("Compare enumerables of type " + typeof(T).Name);
            if (a.SequenceEqual(b, comparer))
            {
                return true;
            }
            else if (matchSequence)
            {
                return false;
            }
            else
            {
                // easiest way to check if they're the same:
                // 1. check if they have the same number of items - if they don't, then obviously they're not equal
                // 2. if they have the same number, then we'll just iterate through the first and check that the second
                //    has each item. if there's an item that's not found, then obviously they're not equal
                // 3. finally, we'll do the same by iterating through the second and checking against the first
                //
                // this way, I'm avoiding iterating through both and doing any further math or calculations
                // the second iteration is annoying overkill, but it helps avoid an edge case where they could differ
                int aCount = a.Count();
                int bCount = b.Count();
                if (aCount == bCount)
                {
                    // compare each item, checking b against a
                    foreach (T item in a)
                    {
                        if (b.Any(i => comparer.Equals(item, i)))
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    // second comparison, checking a against b
                    foreach (T item in b)
                    {
                        if (a.Any(i => comparer.Equals(item, i)))
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// If present, will prevent <see cref="CompareObjects{T}(T, T)"/> from checking and comparing this property or field.
        /// This can be used for properties or fields that are meant to be unique per object.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        public class DoNotCompareAttribute : Attribute
        {

        }
    }

    /// <summary>
    /// A class indicating the result of a comparison done via the <see cref="Compare"/> class.
    /// </summary>
    public class CompareResult
    {
        public CompareResult()
        {

        }

        public CompareResult(CompareSuccessValue result, bool equal = false)
        {
            Success = result;
            Equal = equal;
        }

        public CompareResult(bool equal)
        {
            Equal = equal;
            Success = CompareSuccessValue.Success;
        }

        public CompareResult(List<string> properties)
        {
            Equal = properties.Count == 0;
            Success = CompareSuccessValue.Success;
            DifferingProperties = properties;
        }

        /// <summary>
        /// Get if a comparison could be successfully completed.
        /// If the comparison was successful, then this will be <see cref="CompareSuccessValue.Success"/>.
        /// If either or both objects were null, then this will be <see cref="CompareSuccessValue.NullObject"/>.
        /// Otherwise, if a comparison was attempted between two objects that aren't supported, then thos will be
        /// <see cref="CompareSuccessValue.NotSupported"/>.
        /// </summary>
        public CompareSuccessValue Success { get; private set; } = CompareSuccessValue.Success;

        /// <summary>
        /// Get if the two objects compared were equal.
        /// </summary>
        /// <remarks>
        /// If <see cref="Success"/> is set to <see cref="CompareSuccessValue.NotSupported"/>, then this will always be <c>false</c>.
        /// If <see cref="Success"/> is set to <see cref="CompareSuccessValue.NullObject"/>, then this will be <c>true</c>
        /// if both objects were null.
        /// </remarks>
        public bool Equal { get; private set; } = false;

        /// <summary>
        /// The list of properties or fields in the objects that have different values, if any.
        /// </summary>
        public List<string> DifferingProperties { get; private set; } = new List<string>();
    }

    /// <summary>
    /// A value indicating if a comparison done via the <see cref="Compare"/> class was successful or not.
    /// </summary>
    public enum CompareSuccessValue
    {
        /// <summary>
        /// The comparison was able to be completed successfully.
        /// </summary>
        Success = 0,
        /// <summary>
        /// One or both of the compared objects are <c>null</c>. Use <see cref="CompareResult.Equal"/> to determine if both are null.
        /// </summary>
        NullObject = 1,
        /// <summary>
        /// A comparison was attempted on two objects that are not supported, such as a primitive type, a value type, an enum, or an IEnumerable object.
        /// </summary>
        NotSupported = 2,
    }
}
