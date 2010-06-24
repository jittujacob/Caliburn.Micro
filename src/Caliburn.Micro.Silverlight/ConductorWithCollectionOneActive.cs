﻿namespace Caliburn.Micro
{
    using System;
    using System.Linq;

    public partial class Conductor<T>
    {
        /// <summary>
        /// An implementation of <see cref="IConductor"/> that holds on many items.
        /// </summary>
        public class Collection
        {
            /// <summary>
            /// An implementation of <see cref="IConductor"/> that holds on many items but only activates on at a time.
            /// </summary>
            public class OneActive : ConductorBase<T>
            {
                readonly BindableCollection<T> items = new BindableCollection<T>();

                /// <summary>
                /// Gets the items that are currently being conducted.
                /// </summary>
                public BindableCollection<T> Items
                {
                    get { return items; }
                }

                /// <summary>
                /// Activates the specified item.
                /// </summary>
                /// <param name="item">The item to activate.</param>
                public override void ActivateItem(T item)
                {
                    if(item != null && item.Equals(ActiveItem))
                        return;

                    ChangeActiveItem(item, false);
                }

                /// <summary>
                /// Closes the specified item.
                /// </summary>
                /// <param name="item">The item to close.</param>
                public override void CloseItem(T item)
                {
                    if(item == null)
                        return;

                    var guard = item as IGuardClose;

                    if(guard == null)
                        CloseItemCore(item);
                    else
                    {
                        guard.CanClose(result =>{
                            if(result)
                                CloseItemCore(item);
                        });
                    }
                }

                void CloseItemCore(T item)
                {
                    if(item.Equals(ActiveItem))
                    {
                        var index = Items.IndexOf(item);
                        var next = DetermineNextItemToActivate(index);
                        
                        ChangeActiveItem(next, true);
                    }
                    else
                    {
                        var deactivator = item as IDeactivate;
                        if (deactivator != null)
                            deactivator.Deactivate(true);
                    }

                    Items.Remove(item);
                }

                /// <summary>
                /// Determines the next item to activate based on the last active index.
                /// </summary>
                /// <param name="lastIndex">The index of the last active item.</param>
                /// <returns>The next item to activate.</returns>
                /// <remarks>Called after an active item is closed.</remarks>
                protected virtual T DetermineNextItemToActivate(int lastIndex)
                {
                    var toRemoveAt = lastIndex - 1;

                    if(toRemoveAt == -1 && Items.Count > 1)
                        return Items[1];
                    if(toRemoveAt > -1 && toRemoveAt < Items.Count - 1)
                        return Items[toRemoveAt];
                    return default(T);
                }

                /// <summary>
                /// Called to check whether or not this instance can close.
                /// </summary>
                /// <param name="callback">The implementor calls this action with the result of the close check.</param>
                public override void CanClose(Action<bool> callback)
                {
                    new CompositeCloseStrategy<T>(Items.GetEnumerator(), callback).Execute();
                }

                /// <summary>
                /// Called when activating.
                /// </summary>
                protected override void OnActivate()
                {
                    var activator = ActiveItem as IActivate;

                    if(activator != null)
                        activator.Activate();
                }

                /// <summary>
                /// Called when deactivating.
                /// </summary>
                /// <param name="close">Inidicates whether this instance will be closed.</param>
                protected override void OnDeactivate(bool close)
                {
                    if(close)
                        items.OfType<IDeactivate>().Apply(x => x.Deactivate(true));
                    else
                    {
                        var deactivator = ActiveItem as IDeactivate;

                        if(deactivator != null)
                            deactivator.Deactivate(false);
                    }
                }

                /// <summary>
                /// Ensures that an item is ready to be activated.
                /// </summary>
                /// <param name="newItem"></param>
                /// <returns>The item to be activated.</returns>
                protected override T EnsureItem(T newItem)
                {
                    if (newItem == null)
                        newItem = DetermineNextItemToActivate(ActiveItem != null ? Items.IndexOf(ActiveItem) : 0);
                    else
                    {
                        var index = Items.IndexOf(newItem);

                        if (index == -1)
                            Items.Add(newItem);
                        else newItem = Items[index];
                    }

                    return base.EnsureItem(newItem);
                }
            }
        }
    }
}