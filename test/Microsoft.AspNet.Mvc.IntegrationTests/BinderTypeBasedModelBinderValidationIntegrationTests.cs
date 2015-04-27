// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Collections;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class BinderTypeBasedModelBinderValidationIntegrationTests
    {
        private class Person
        {
            public IFormFile File { get; set; }
        }

        [Fact(Skip = "Fix Bugs #2445, #2446 to enable this test.")]
        public async Task BindProperty_WithData_GetsBound_ValidatesTopLevelModel()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    UpdateRequest(request, data, "File");
                });

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
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.File);
            Assert.Equal("form-data; name=File; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(boundPerson.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            var key = Assert.Single(modelState.Keys); // Should be only one key. bug #2446
            Assert.Equal("File", key);
            Assert.NotNull(modelState[key].Value); // should be non null bug #2445.
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact(Skip = "Fix #2456 to enable this test case.")]
        public async Task BindParameter_NoData_DoesNotGetBound()
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

                ParameterType = typeof(IFormFile)
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.ContentType = "multipart/form-data";
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult); // Fails due to bug #2456
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        [Fact(Skip = "Fix #2446 to enable this test case.")]
        public async Task BindParameter_WithData_GetsBound_ValidationIsSkipped()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    // Setting a custom parameter prevents it from falling back to an empty prefix.
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(IFormFile)
            };

            var data = "Some Data Is Better Than No Data.";
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    UpdateRequest(request, data, "CustomParameter");
                });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var file = Assert.IsType<FormFile>(modelBindingResult.Model);
            Assert.NotNull(file);
            Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);

            // Validation should be skipped because we do not validate any parameters and since IFormFile is not
            // IValidatableObject, we should have no entries in the model state dictionary.
            Assert.Empty(modelState.Keys); // Enable when we fix #2446.
        }

        private void UpdateRequest(HttpRequest request, string data, string name)
        {
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, string[]>(), fileCollection);
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            request.Form = formCollection;
            request.ContentType = "multipart/form-data";
            request.Headers["Content-Disposition"] = "form-data; name=" + name + "; filename=text.txt";
            fileCollection.Add(new FormFile(memoryStream, 0, data.Length)
            {
                Headers = request.Headers
            });
        }
    }
}