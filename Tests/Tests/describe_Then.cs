﻿using NSpec;
using Promises;
using System.Threading;
using System;

class describe_Then : nspec {

    const int shortDuration = 5;
    const int actionDuration = 10;
    const int actionDurationPlus = 15;

    void when_then() {
        Promise<int> firstPromise = null;
        Promise<string> thenPromise = null;

        context["when first promise fulfilles"] = () => {
            before = () => {
                firstPromise = TestHelper.PromiseWithResult(42, actionDuration);
                thenPromise = firstPromise.Then(result => {
                    Thread.Sleep(actionDuration);
                    return result + "_expensive";
                });
            };

            context["initial state"] = () => {
                it["first promise in initial state"] = () => assertInitialState(firstPromise, 0f);
                it["then promise is pending"] = () => assertPending(thenPromise);
            };

            context["after first promise fulfilled"] = () => {
                before = () => Thread.Sleep(actionDurationPlus);
                it["first promise is fulfilled"] = () => assertFulfilledState(firstPromise, 42);
                it["then promise in initial state"] = () => assertInitialState(thenPromise, 0.5f);

                context["after then promise fulfilled"] = () => {
                    before = () => Thread.Sleep(actionDuration);
                    it["then promise in fulfilled state"] = () => assertFulfilledState(thenPromise, "42_expensive");
                };
            };
        };

        context["when first promise fails"] = () => {
            before = () => {
                firstPromise = TestHelper.PromiseWithError<int>("error 42", actionDuration);
                thenPromise = firstPromise.Then(result => {
                    Thread.Sleep(actionDuration);
                    return result + "_expensive";
                });
            };

            context["after first promise failed"] = () => {
                before = () => Thread.Sleep(actionDurationPlus);
                it["first promise failed"] = () => assertFailedState(firstPromise, "error 42");
                it["then promise failed"] = () => assertFailedState(thenPromise, "error 42");
            };
        };

        context["when chaining"] = () => {
            it["fulfills"] = () => {
                var promise = Promise<string>.PromiseWithAction(() => "1")
                    .Then(result => result + "2")
                    .Then(result => result + "3")
                    .Then(result => result + "4");

                var fulfilled = 0;
                promise.OnFulfilled += result => fulfilled++;
                Thread.Sleep(shortDuration);
                fulfilled.should_be(1);
                promise.result.should_be("1234");
            };

            it["forwards error"] = () => {
                var promise = TestHelper.PromiseWithError<string>("error 42")
                    .Then(result => result + "2")
                    .Then(result => result + "3")
                    .Then(result => result + "4");

                var fulfilled = false;
                Exception eventError = null;
                promise.OnFulfilled += result => fulfilled = true;
                promise.OnFailed += error => eventError = error;
                Thread.Sleep(shortDuration);
                fulfilled.should_be_false();
                eventError.Message.should_be("error 42");
                promise.result.should_be_null();
                promise.error.Message.should_be("error 42");
            };

            it["calculates correct progress"] = () => {
                var promise = Promise<string>.PromiseWithAction(() => "1")
                    .Then(result => result + "2")
                    .Then(result => result + "3")
                    .Then<string>(result => {
                        throw new Exception("error 42");
                    });

                Thread.Sleep(actionDuration);
                promise.progress.should_be(0.75f);
            };
        };
    }

    void assertInitialState<TResult>(Promise<TResult> p, float progress) {
        p.state.should_be(PromiseState.Unfulfilled);
        p.error.should_be_null();
        p.progress.should_be(progress);
        p.thread.should_not_be_null();

        if (p.result is int)
            p.result.should_be(0);
        else
            ((object)p.result).should_be_null();
    }

    void assertPending<TResult>(Promise<TResult> p) {
        p.state.should_be(PromiseState.Unfulfilled);
        p.error.should_be_null();
        p.progress.should_be(0f);
        p.thread.should_be_null();

        if (p.result is int)
            p.result.should_be(0);
        else
            ((object)p.result).should_be_null();
    }

    void assertFulfilledState<TResult>(Promise<TResult> p, TResult result) {
        p.state.should_be(PromiseState.Fulfilled);
        p.error.should_be_null();
        p.progress.should_be(1f);
        p.thread.should_be_null();
        p.result.should_be(result);
    }

    void assertFailedState<TResult>(Promise<TResult> p, string errorMessage) {
        p.state.should_be(PromiseState.Failed);
        p.error.should_not_be_null();
        p.error.Message.should_be(errorMessage);
        p.thread.should_be_null();

        if (p.result is int)
            p.result.should_be(0);
        else
            ((object)p.result).should_be_null();
    }
}

