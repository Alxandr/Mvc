// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class HeaderValidationIntegrationTests
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            [FromHeader(Name = "Header")]
            [Required]
            public string Street { get; set; }
        }

        [Fact]
        public async Task MissingHeader_UsesFullPathAsKey_ForModelStateErrors()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(Person)
            };

            // Do not add any headers.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request => { });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson);

            // ModelState
            Assert.False(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter.Address.Header", key);
            var error = Assert.Single(modelState[key].Errors);
            Assert.Equal("The Street field is required.", error.ErrorMessage);
        }

        [Theory]
        // [InlineData(typeof(string[]), "value1, value2, value3")] Fix #2446 to enable this scenario.
        [InlineData(typeof(string), "value")]
        public async Task HeaderPresent_ModelGetsBound(Type modelType, string value)
        {
            // Arrange
            var expectedValue = value.Split(',').Select(v => v.Trim());

            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Header
                },
                ParameterType = modelType
            };

            Action<HttpRequest> action = (r) => r.Headers.Add("CustomParameter", new[] { value });
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(action);

            // Do not add any headers.
            var httpContext = operationContext.HttpContext;
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            Assert.NotNull(modelBindingResult.Model);
            Assert.IsType(modelType, modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter", key);

            // Enable when #2445 gets fixed.
            //Assert.NotNull(modelState[key].Value);
            //Assert.Equal(expectedValue, modelState[key].Value.RawValue);
            //Assert.Equal(value, modelState[key].Value.AttemptedValue);
        }
    }
}