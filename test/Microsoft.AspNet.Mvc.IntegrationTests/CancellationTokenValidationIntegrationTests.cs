// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class CancellationTokenValidationIntegrationTests
    {
        private class Person
        {
            public CancellationToken Token { get; set; }
        }

        [Fact(Skip = "Fix #2447 To Enable the test")]
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
            Assert.NotNull(boundPerson.Token);

            // ModelState
            Assert.True(modelState.IsValid);

            Assert.Equal(2, modelState.Keys.Count);
            Assert.Single(modelState.Keys, k => k == "CustomParameter");

            var key = Assert.Single(modelState.Keys, k => k == "CustomParameter.Token");
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);

            // This Assert Fails.
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
        }

        [Fact(Skip = "Fix #2447 To Enable the test")]
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
                },

                ParameterType = typeof(CancellationToken)
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
            var token = Assert.IsType<CancellationToken>(modelBindingResult.Model);
            Assert.NotNull(token);

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys);
            Assert.Equal("CustomParameter", key);
            Assert.Null(modelState[key].Value);
            Assert.Empty(modelState[key].Errors);

            // This assert fails.
            Assert.Equal(ModelValidationState.Skipped, modelState[key].ValidationState);
        }
    }
}