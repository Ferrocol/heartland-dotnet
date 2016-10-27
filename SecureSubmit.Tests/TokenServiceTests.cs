﻿using SecureSubmit.Entities;
using SecureSubmit.Services;
using SecureSubmit.Tests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace SecureSubmit.Tests
{
    [TestClass]
    public class TokenServiceTests
    {
        private HpsTokenService _tokenService;
        private HpsCreditCard _card;

        [TestInitialize]
        public void Init() {
            _tokenService = new HpsTokenService("pkapi_cert_jKc1FtuyAydZhZfbB3");
            _card = new HpsCreditCard {
                Cvv = "123",
                ExpMonth = 12,
                ExpYear = 2025,
                Number = "4012002000060016"
            };
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Key_NullPublicKey_ShouldThrowArgumentNullException()
        {
            new HpsTokenService(null);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Key_EmptyPublicKey_ShouldThrowArgumentNullException()
        {
            new HpsTokenService("");
        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Key_BadFormedPublicKey_ShouldThrowArgumentException()
        {
            new HpsTokenService("pkapi_bad");
        }

        [TestMethod, ExpectedException(typeof(WebException))]
        public void Key_BadPublicKey_ShouldThrowArgumentException()
        {
            new HpsTokenService("pkapi_foo_foo").GetToken(_card);
        }

        [TestMethod]
        public void Validation_InvalidCardNumber_ShouldReturnError()
        {
            var card = new HpsCreditCard { Number = "1" };

            var response = _tokenService.GetToken(card);
            var error = response.error;

            Assert.IsNotNull(error);
            Assert.AreEqual("2", error.code);
            Assert.AreEqual("card.number", error.param);
            Assert.AreEqual("Card number is invalid.", error.message);
        }

        [TestMethod]
        public void Validation_TooLongCardNumber_ShouldReturnError()
        {
            var card = new HpsCreditCard { Number = "11111111111111111111111111111111111" };

            var response = _tokenService.GetToken(card);
            var error = response.error;

            Assert.IsNotNull(error);
            Assert.AreEqual("2", error.code);
            Assert.AreEqual("card.number", error.param);
            Assert.AreEqual("Card number is invalid.", error.message);
        }

        [TestMethod]
        public void Validation_TooHighExpirationMonth_ShouldReturnError()
        {
            _card.ExpMonth = 13;
            var response = _tokenService.GetToken(_card);
            var error = response.error;

            Assert.IsNotNull(error);
            Assert.AreEqual("2", error.code);
            Assert.AreEqual("card.exp_month", error.param);
            Assert.AreEqual("Card expiration month is invalid.", error.message);
        }

        [TestMethod]
        public void Validation_TooLowExpirationYear_ShouldReturnError()
        {
            _card.ExpYear = 12;

            var response = _tokenService.GetToken(_card);
            var error = response.error;

            Assert.IsNotNull(error);
            Assert.AreEqual("2", error.code);
            Assert.AreEqual("card.exp_year", error.param);
            Assert.AreEqual("Card expiration year is invalid.", error.message);
        }

        [TestMethod]
        public void Validation_YearUnder2000_ShouldReturnError()
        {
            _card.ExpYear = 1999;

            var response = _tokenService.GetToken(_card);
            var error = response.error;

            Assert.IsNotNull(error);
            Assert.AreEqual("2", error.code);
            Assert.AreEqual("card.exp_year", error.param);
            Assert.AreEqual("Card expiration year is invalid.", error.message);
        }

        [TestMethod]
        public void TokenResult_ValidConfiguration_ShouldHaveNullError()
        {
            var response = _tokenService.GetToken(_card);
            Assert.IsNull(response.error);
        }

        [TestMethod]
        public void TokenResult_WhenValid_TokenDataShouldBePresent()
        {
            var response = _tokenService.GetToken(_card);
            Assert.IsNotNull(response.token_value);
            Assert.IsNotNull(response.token_type);
            Assert.IsNotNull(response.token_expire);
        }

        [TestMethod]
        public void Integration_WhenTokenIsAcquired_ShouldBeAbleToCharge()
        {
            var response = _tokenService.GetToken(_card);
            var chargeService = new HpsCreditService(new HpsServicesConfig { SecretApiKey = "skapi_cert_MTyMAQBiHVEAewvIzXVFcmUd2UcyBge_eCpaASUp0A" });
            var charge = chargeService.Charge(1, "usd", response.token_value, TestCardHolder.ValidCardHolder);

            Assert.IsNotNull(charge.AuthorizationCode);
        }
    }
}
