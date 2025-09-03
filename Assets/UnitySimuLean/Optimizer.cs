using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimuLean
{
    /// <summary>
    /// Simple optimizer component that mirrors the inspector
    /// configuration style used by <see cref="Experimenter"/>.
    /// It exposes lists of input and output variables so they can
    /// be assigned from the Unity editor.
    /// </summary>
    public class Optimizer : MonoBehaviour
    {
        [Header("Input Variables")]
        [SerializeField] private List<SElement> iElements = new();
        [SerializeField] private List<string> iParameters = new();

        [Header("Output Variables")]
        [SerializeField] private List<SElement> oElements = new();
        [SerializeField] private List<string> variables = new();

        /// <summary>
        /// Sets the value of a property identified by <paramref name="propertyName"/>
        /// on the supplied <paramref name="obj"/>. Only <c>int</c> and <c>float</c>
        /// properties are supported since they are the numeric types commonly
        /// used by the simulation elements.
        /// </summary>
        public static void SetPropertyValue(object obj, string propertyName, float value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                if (property.PropertyType == typeof(int))
                    property.SetValue(obj, (int)value);
                else if (property.PropertyType == typeof(float))
                    property.SetValue(obj, value);
                else
                    throw new ArgumentException($"Property '{propertyName}' no es int ni float.");
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' no encontrada o de solo lectura.");
            }
        }

        /// <summary>
        /// Obtains the numeric value of a property identified by
        /// <paramref name="propertyName"/>. Returns <c>null</c> when the
        /// property is missing or cannot be read.
        /// </summary>
        public static int? GetPropertyValue(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    if (property.PropertyType == typeof(int))
                        return (int)value;
                    if (property.PropertyType == typeof(float))
                        return Mathf.RoundToInt((float)value);
                    throw new ArgumentException($"Property '{propertyName}' no es int ni float.");
                }
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' no encontrada o de solo escritura.");
            }
            return null;
        }
    }
}

