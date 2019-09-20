﻿// <copyright file="CreateSendingNotificationActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;

    /// <summary>
    /// This class contains the "create sending notification data entity" durable activity.
    /// </summary>
    public class CreateSendingNotificationActivity
    {
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSendingNotificationActivity"/> class.
        /// </summary>
        /// <param name="sendingNotificationDataRepository">Sending notification data repository.</param>
        /// <param name="adaptiveCardCreator">The adaptive card creator.</param>
        public CreateSendingNotificationActivity(
            SendingNotificationDataRepository sendingNotificationDataRepository,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.adaptiveCardCreator = adaptiveCardCreator;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 3);

            await context.CallActivityWithRetryAsync(
                nameof(CreateSendingNotificationActivity.CreateSendingNotificationAsync),
                retryOptions,
                notificationDataEntity);
        }

        /// <summary>
        /// This method represents the "create sending notification" duralbe activity.
        /// </summary>
        /// <param name="notificationDataEntity">A notification to be sent to reipients.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(CreateSendingNotificationAsync))]
        public async Task CreateSendingNotificationAsync(
            [ActivityTrigger] NotificationDataEntity notificationDataEntity)
        {
            var cardString = this.adaptiveCardCreator.CreateAdaptiveCard(notificationDataEntity).ToJson();

            var sendingNotification = new SendingNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SendingNotificationsPartition,
                RowKey = notificationDataEntity.RowKey,
                NotificationId = notificationDataEntity.RowKey,
                Content = cardString,
            };

            await this.sendingNotificationDataRepository.CreateOrUpdateAsync(sendingNotification);
        }
    }
}
