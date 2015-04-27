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
    public class ServiceValidationIntegrationTests
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            // Using a service type already in defaults.
            [FromServices]
            public JsonOutputFormatter OutputFormatter { get; set; }
        }

        [Fact]
        public async Task BindProperty_ServicePresent_GetsBound_DoesNotValidate()
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

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(httpContext => { });
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
            Assert.NotNull(boundPerson.Address.OutputFormatter);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(3, modelState.Keys.Count);
            Assert.Single(modelState.Keys, k => k == "CustomParameter");
            Assert.Single(modelState.Keys, k => k == "CustomParameter.Address");

            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Address.OutputFormatter");
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);
        }

        [Fact]
        public async Task BindParameter_ServicePresent_GetsBound_DoesNotValidate()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                    BindingSource = BindingSource.Services
                },

                // Using a service type already in defaults.
                ParameterType = typeof(JsonOutputFormatter)
            };

            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(httpContext => { });
            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var outputFormatter = Assert.IsType<JsonOutputFormatter>(modelBindingResult.Model);
            Assert.NotNull(outputFormatter);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter", key);
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);
        }
    }
}