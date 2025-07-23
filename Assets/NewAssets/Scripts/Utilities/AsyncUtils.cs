using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Utilities
{
    /// <summary>
    /// Utility class for converting between Unity Coroutines and .NET Tasks
    /// </summary>
    public static class AsyncUtils
    {
        /// <summary>
        /// Converts a Task to a Coroutine that can be yielded in Unity
        /// </summary>
        /// <param name="task">The task to convert</param>
        /// <returns>A coroutine that waits for the task to complete</returns>
        public static IEnumerator TaskAsCoroutine(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception?.InnerException ?? task.Exception;
        }

        /// <summary>
        /// Converts a Task to a Coroutine that can be yielded in Unity
        /// </summary>
        /// <typeparam name="T">The return type of the task</typeparam>
        /// <param name="task">The task to convert</param>
        /// <returns>A coroutine that waits for the task to complete</returns>
        public static IEnumerator TaskAsCoroutine<T>(Task<T> task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception?.InnerException ?? task.Exception;
        }

        /// <summary>
        /// Converts a Coroutine to a Task with cancellation support
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to run the coroutine on</param>
        /// <param name="coroutine">The coroutine to convert</param>
        /// <param name="cancellationToken">Cancellation token to stop the coroutine</param>
        /// <returns>A task that completes when the coroutine finishes or is canceled</returns>
        public static Task CoroutineAsTask(MonoBehaviour monoBehaviour, IEnumerator coroutine, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<bool> tcs = new();
            Coroutine runningCoroutine = null;

            // Register cancellation callback to stop the coroutine
            var cancellationRegistration = cancellationToken.Register(() =>
            {
                if (runningCoroutine != null)
                {
                    monoBehaviour.StopCoroutine(runningCoroutine);
                }
                tcs.TrySetCanceled(cancellationToken);
            });

            // Start the coroutine and store its reference
            runningCoroutine = monoBehaviour.StartCoroutine(WrapCoroutineWithCompletion(coroutine, tcs, cancellationToken, cancellationRegistration));
            return tcs.Task;
        }

        /// <summary>
        /// Converts a Coroutine to a Task with a return value and cancellation support
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to run the coroutine on</param>
        /// <param name="coroutine">The coroutine to convert</param>
        /// <param name="resultSelector">Function to extract the result from the coroutine</param>
        /// <param name="cancellationToken">Cancellation token to stop the coroutine</param>
        /// <returns>A task that completes with a result when the coroutine finishes or is canceled</returns>
        public static Task<T> CoroutineAsTask<T>(MonoBehaviour monoBehaviour, IEnumerator coroutine, Func<T> resultSelector, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<T> tcs = new();
            Coroutine runningCoroutine = null;

            // Register cancellation callback to stop the coroutine
            var cancellationRegistration = cancellationToken.Register(() =>
            {
                if (runningCoroutine != null)
                {
                    monoBehaviour.StopCoroutine(runningCoroutine);
                }
                tcs.TrySetCanceled(cancellationToken);
            });

            // Start the coroutine and store its reference
            runningCoroutine = monoBehaviour.StartCoroutine(WrapCoroutineWithCompletionAndResult(coroutine, tcs, resultSelector, cancellationToken, cancellationRegistration));
            return tcs.Task;
        }

        private static IEnumerator WrapCoroutineWithCompletion(IEnumerator coroutine, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken, CancellationTokenRegistration cancellationRegistration)
        {
            try
            {
                yield return coroutine;

                // Only set result if not already canceled
                if (!cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetResult(true);
                }
            }
            finally
            {
                // Clean up the cancellation registration
                cancellationRegistration.Dispose();
            }
        }

        private static IEnumerator WrapCoroutineWithCompletionAndResult<T>(IEnumerator coroutine, TaskCompletionSource<T> tcs, Func<T> resultSelector, CancellationToken cancellationToken, CancellationTokenRegistration cancellationRegistration)
        {
            try
            {
                yield return coroutine;

                // Only set result if not already canceled
                if (!cancellationToken.IsCancellationRequested)
                {
                    T result = resultSelector();
                    tcs.TrySetResult(result);
                }
            }
            finally
            {
                // Clean up the cancellation registration
                cancellationRegistration.Dispose();
            }
        }
    }
}
