using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using StatLight.Core.Configuration;
using StatLight.Core.Events;
using StatLight.Core.Tests;

namespace StatLight.IntegrationTests.ProviderTests.MSTest
{
    [TestFixture]
    public class when_testing_the_runner_with_MSTest_tests
        : IntegrationFixtureBase
    {
        private ClientTestRunConfiguration _clientTestRunConfiguration;
        private InitializationOfUnitTestHarnessClientEvent _initializationOfUnitTestHarnessClientEvent;

        private readonly IList<TestExecutionClassCompletedClientEvent> _testExecutionClassCompletedClientEvent = new List<TestExecutionClassCompletedClientEvent>();
        private readonly IList<TestExecutionMethodIgnoredClientEvent> _testExecutionMethodIgnoredClientEvent = new List<TestExecutionMethodIgnoredClientEvent>();
        private readonly IList<TestExecutionMethodFailedClientEvent> _testExecutionMethodFailedClientEvent = new List<TestExecutionMethodFailedClientEvent>();
        private readonly IList<TestExecutionMethodPassedClientEvent> _testExecutionMethodPassedClientEvent = new List<TestExecutionMethodPassedClientEvent>();

        protected override string GetTestXapPath()
        {
            return TestXapFileLocations.MSTestSL5;
        }

        protected override ClientTestRunConfiguration ClientTestRunConfiguration
        {
            get { return _clientTestRunConfiguration ?? (_clientTestRunConfiguration = new IntegrationTestClientTestRunConfiguration()); }
        }

        protected override void Before_all_tests()
        {
            base.Before_all_tests();

            EventSubscriptionManager.AddListenerAction<InitializationOfUnitTestHarnessClientEvent>(e => _initializationOfUnitTestHarnessClientEvent = e);
            EventSubscriptionManager.AddListenerAction<TestExecutionClassCompletedClientEvent>(e => _testExecutionClassCompletedClientEvent.Add(e));
            EventSubscriptionManager.AddListenerAction<TestExecutionMethodIgnoredClientEvent>(e => _testExecutionMethodIgnoredClientEvent.Add(e));
            EventSubscriptionManager.AddListenerAction<TestExecutionMethodFailedClientEvent>(e => _testExecutionMethodFailedClientEvent.Add(e));
            EventSubscriptionManager.AddListenerAction<TestExecutionMethodPassedClientEvent>(e => _testExecutionMethodPassedClientEvent.Add(e));
        }

        [Test]
        public void Should_have_correct_TotalFailed_count()
        {
#if DEBUG
            TestReport.TotalFailed.ShouldEqual(5);
#else
            TestReport.TotalFailed.ShouldEqual(4);
#endif
        }

        [Test]
        public void Should_have_correct_TotalPassed_count_except_theres_one_extra_passed_test_here_because_of_the_MessageBox_test()
        {
            TestReport.TotalPassed.ShouldEqual(9);
        }

        [Test]
        public void Should_have_correct_TotalIgnored_count()
        {
            TestReport.TotalIgnored.ShouldEqual(1);
        }

        #region Events

        [Test]
        public void Should_receive_one_InitializationOfUnitTestHarness()
        {
            _initializationOfUnitTestHarnessClientEvent.ShouldNotBeNull();
        }

        [Test]
        public void Should_receive_the_TestExecutionClassCompletedClientEvent()
        {
            _testExecutionClassCompletedClientEvent.Count().ShouldEqual(2);
            _testExecutionClassCompletedClientEvent.Each(AssertTestExecutionClassData);
        }

        [Test]
        public void Should_receive_the_TestExecutionMethodIgnoredClientEvent()
        {
            _testExecutionMethodIgnoredClientEvent.Count().ShouldEqual(1);
            _testExecutionMethodIgnoredClientEvent.First().MethodName.ShouldEqual("this_should_be_an_Ignored_test");
            _testExecutionMethodIgnoredClientEvent.First().Message.ShouldEqual("this_should_be_an_Ignored_test");
            //AssertTestExecutionClassData(_testExecutionMethodIgnoredClientEvent.First());
            //TODO: figure out how to get the class/namespace for the ignored test.
        }

        [Test]
        public void Should_receive_the_TestExecutionMethodFailedClientEvent()
        {
            _testExecutionMethodFailedClientEvent.Count().ShouldEqual(3);

            var e = _testExecutionMethodFailedClientEvent.First();

            AssertTestExecutionClassData(e);
            //TODO: assert other properties of the failed exception?

            e.Finished.ShouldNotEqual(new DateTime());
            e.Started.ShouldNotEqual(new DateTime());
        }

        [Test]
        public void Should_receive_the_TestExecutionMethodPassedClientEvent()
        {
#if DEBUG
            _testExecutionMethodPassedClientEvent.Count.ShouldEqual(10);
#else
            _testExecutionMethodPassedClientEvent.Count.ShouldEqual(9);
#endif
        }

        private static void AssertTestExecutionClassData(TestExecutionClassClientEvent e)
        {
            e.NamespaceName.ShouldEqual("StatLight.IntegrationTests.Silverlight", "{0} - NamespaceName property should be correct.".FormatWith(e.GetType().FullName));

            var validClassNames = new List<string> { "MSTestTests+MSTestNestedClassTests", "MSTestTests" };
            if (!validClassNames.Contains(e.ClassName))
                Assert.Fail("e.ClassName is not equal to MSTestNestedClassTests or MSTestTest - actual=" + e.ClassName);
        }
        #endregion

        [Test]
        public void Should_have_reported_a_timeout_failure_correctly()
        {
            var failedTimeoutResult = TestReport.TestResults.SingleOrDefault(w => w.HasExceptionInfoWithCriteria(ex => ex.FullMessage.Contains("Timeout")));
            failedTimeoutResult.ShouldNotBeNull();
        }
#if DEBUG
        [Test]
        public void Should_have_reported_a_debug_assertion_error()
        {
            var assertionResult = TestReport
                .TestResults
                .Single(w => (w.MethodName != null ? w.MethodName.Equals("Should_fail_due_to_a_dialog_assertion") : false));

            assertionResult
                .OtherInfo
                .ShouldContain("Debug Assertion")
                .ShouldContain("Should_fail_due_to_a_dialog_assertion - message");

            assertionResult.ResultType.ShouldEqual(ResultType.Failed);
        }
#endif
        [Test]
        public void Should_have_scraped_the__messageBox_overload_1__test_message_box_info()
        {
            var nonEmptyOtherInfoResults = TestReport.TestResults.Where(w => !string.IsNullOrEmpty(w.OtherInfo));
            var theOneWeWant = nonEmptyOtherInfoResults.Single(w => w.OtherInfo.Contains("Should_fail_due_to_a_message_box_modal_dialog"));
            theOneWeWant.OtherInfo.ShouldContain("Should_fail_due_to_a_message_box_modal_dialog - message");

            theOneWeWant.ResultType.ShouldEqual(ResultType.SystemGeneratedFailure);
        }

        [Test]
        public void Should_have_pulled_the_DescriptionAttribute_information_out_of_a_failing_test()
        {
            TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("this_should_be_a_Failing_test"))
                .Each(theOneWeWant => theOneWeWant.ShouldNotBeNull().ReadMetadata("Description").Each(x => x.ShouldEqual("Test description on failing test.")));
        }


        [Test]
        public void Should_have_pulled_the_OwnerAttribute_information_out_of_a_failing_test()
        {
            TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("this_should_be_a_Failing_test"))
                .Each(theOneWeWant => theOneWeWant.ShouldNotBeNull().ReadMetadata("Owner").Each(x => x.ShouldEqual("SomeOwnerString")));

        }


        [Test]
        public void Should_have_pulled_the_DescriptionAttribute_information_out_of_a_passing_test()
        {
            TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("this_should_be_a_passing_test") && w.ClassName.Equals("MSTestTests"))
                .Each(theOneWeWant => theOneWeWant.ShouldNotBeNull().ReadMetadata("Description").Each(x => x.ShouldEqual("Test description on failing test.")));
        }


        [Test]
        public void Should_have_pulled_the_OwnerAttribute_information_out_of_a_passing_test()
        {
            TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("this_should_be_a_passing_test") && w.ClassName.Equals("MSTestTests"))
                .Each(theOneWeWant => theOneWeWant.ShouldNotBeNull().ReadMetadata("Owner").Each(x => x.ShouldEqual("SomeOwnerString")));
        }


        [Test]
        public void Should_have_pulled_the_PropertyAttribute_information_out_of_a_passing_test()
        {
            TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("this_should_be_a_passing_test") && w.ClassName.Equals("MSTestTests"))
                .Each(theOneWeWant => theOneWeWant.ShouldNotBeNull().ReadMetadata("tpName").Each(x => x.ShouldEqual("tpValue")));
        }

        [Test]
        public void Should_have_pulled_the_TestContext_WriteLine_information_and_be_in_the_correct_order()
        {
            TestCaseResultServerEvent testCaseResultServerEvent = TestReport
                .TestResults
                .Where(w => w.MethodName.Equals("Should_be_able_to_write_to_the_TestContext") && w.ClassName.Equals("MSTestTests"))
                .Single();

            testCaseResultServerEvent.Metadata.Count().ShouldBeGreaterThan(0, "Should have found some metadata");

            var sb = new StringBuilder();
            var allItems = testCaseResultServerEvent.Metadata.ToList();
            bool failed = false;
            for (int i = 0; i < allItems.Count; i++)
            {

                var value = allItems[i].Value;
                if (value != "Test {0}".FormatWith(i))
                {
                    sb.AppendLine("Expected: Test {0} But Was: {1}".FormatWith(i, value));
                    failed = true;
                }
                else
                {
                    sb.AppendLine(value);
                }
            }

            if (failed)
            {
                Assert.Fail(sb.ToString());
            }
        }
    }

    internal static class AssertionExtensions
    {
        public static bool HasExceptionInfoWithCriteria(this TestCaseResultServerEvent testt, Func<ExceptionInfo, bool> criteria)
        {
            return testt.ExceptionInfo == null ? false : criteria(testt.ExceptionInfo);
        }

        public static IEnumerable<string> ReadMetadata(this TestCaseResultServerEvent testCaseResultServerEvent, string property)
        {
            var data = testCaseResultServerEvent.Metadata.Where(w => w.Name == property);
            if (data.Any())
                return data.Select(s => s.Value);

            return null;
        }
    }
}
