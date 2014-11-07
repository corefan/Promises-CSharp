﻿using UnityEngine;
using Promises;
using System.Collections;

public static class PromiseExtensions {
    public static Promise<T> QueueOnMainThread<T>(this Promise<T> promise,
                                                  Promise<T>.Fulfilled onFulfilled,
                                                  Promise<T>.Failed onFailed = null,
                                                  Promise<T>.Progressed onProgressed = null) {

        PromiseService.AddPromise<T>(promise, onFulfilled, onFailed, onProgressed);
        return promise;
    }
}

class PromiseWithHanlder<T> {
    public float previousProgress;

    public Promise<T> promise { get { return _promise; } }
    public Promise<T>.Fulfilled onFulfilled { get { return _onFulfilled; } }
    public Promise<T>.Failed onFailed { get { return _onFailed; } }
    public Promise<T>.Progressed onProgressed { get { return _onProgressed; } }

    readonly Promise<T> _promise;
    readonly Promise<T>.Fulfilled _onFulfilled;
    readonly Promise<T>.Failed _onFailed;
    readonly Promise<T>.Progressed _onProgressed;

    public PromiseWithHanlder(Promise<T> promise,
                              Promise<T>.Fulfilled onFulfilled = null,
                              Promise<T>.Failed onFailed = null,
                              Promise<T>.Progressed onProgressed = null) {

        _promise = promise;
        _onFulfilled = onFulfilled;
        _onFailed = onFailed;
        _onProgressed = onProgressed;
    }
}

public class PromiseService : MonoBehaviour {
    static PromiseService _service;
    int _promises;

    public static void AddPromise<T>(Promise<T> promise,
                                     Promise<T>.Fulfilled onFulfilled = null,
                                     Promise<T>.Failed onFailed = null,
                                     Promise<T>.Progressed onProgressed = null) {

        Promise<T>.Fulfilled fulfilledHandler = null;
        Promise<T>.Failed failedHandler = null;
        Promise<T>.Progressed progressedHandler = null;

        if (onFulfilled != null) {
            if (promise.state == PromiseState.Fulfilled) {
                onFulfilled(promise.result);
            } else {
                fulfilledHandler = onFulfilled;
            }
        }

        if (onFailed != null) {
            if (promise.state == PromiseState.Failed) {
                onFailed(promise.error);
            } else {
                failedHandler = onFailed;
            }
        }

        if (promise.state == PromiseState.Unfulfilled) {
            if (onProgressed != null) {
                progressedHandler = onProgressed;
                if (promise.progress > 0) {
                    onProgressed(promise.progress);
                }
            }
        }

        if (fulfilledHandler != null || failedHandler != null || progressedHandler != null) {
            getService().addPromiseWithHanlder(new PromiseWithHanlder<T>(
                promise, fulfilledHandler,
                failedHandler, progressedHandler
            ));
        }
    }

    static PromiseService getService() {
        if (_service == null) {
            _service = new GameObject("PromiseService").AddComponent<PromiseService>();
            _service.updateName();
        }

        return _service;
    }

    void addPromiseWithHanlder<T>(PromiseWithHanlder<T> promiseWithHanlder) {
        _promises++;
        updateName();
        StartCoroutine(update(promiseWithHanlder));
    }

    IEnumerator update<T>(PromiseWithHanlder<T> p) {
        while (true) {
            if (p.onProgressed != null) {
                if (p.promise.progress != p.previousProgress) {
                    p.previousProgress = p.promise.progress;
                    p.onProgressed(p.promise.progress);
                }
            }

            if (p.onFulfilled != null && p.promise.state == PromiseState.Fulfilled) {
                p.onFulfilled(p.promise.result);
                _promises--;
                updateName();
                break;
            }
            if (p.onFailed != null && p.promise.state == PromiseState.Failed) {
                p.onFailed(p.promise.error);
                _promises--;
                updateName();
                break;
            }

            yield return new WaitForEndOfFrame();
        }
    }

    void updateName() {
        name = "Promises (" + _promises + " pending)";
    }
}