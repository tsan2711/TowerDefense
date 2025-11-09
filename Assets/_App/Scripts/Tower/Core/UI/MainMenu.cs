using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
	/// <summary>
	/// Abstract base class of a MainMenu
	/// The concrete class should expose serialized fields for the different pages e.g. OptionsMenu
	/// The concrete class should expose methods for change pages that use ChangePage() under the hood. e.g. OpenOptionsMenu()
	/// </summary>
	public abstract class MainMenu : MonoBehaviour
	{
		/// <summary>
		/// Currently open MenuPage
		/// </summary>
		protected IMainMenuPage m_CurrentPage;

		/// <summary>
		/// This stack is to track the pages used to get to specific page - use by the back methods
		/// </summary>
		protected Stack<IMainMenuPage> m_PageStack = new Stack<IMainMenuPage>();

		/// <summary>
		/// Change page
		/// </summary>
		/// <param name="newPage">the page to transition to</param>
		protected virtual void ChangePage(IMainMenuPage newPage)
		{
			DeactivateCurrentPage();
			ActivateCurrentPage(newPage);
		}

		/// <summary>
		/// Deactivates the current page is there is one
		/// </summary>
		protected void DeactivateCurrentPage()
		{
			if (m_CurrentPage != null)
			{
				Debug.Log($"[MainMenu] Deactivating current page: {m_CurrentPage}");
				m_CurrentPage.Hide();
			}
		}

		/// <summary>
		/// Activates the new page, sets it to the current page an adds it to the stack
		/// </summary>
		/// <param name="newPage">the page to be activated</param>
		protected void ActivateCurrentPage(IMainMenuPage newPage)
		{
			Debug.Log($"[MainMenu] Activating page: {newPage}");
			m_CurrentPage = newPage;
			m_CurrentPage.Show();
			m_PageStack.Push(m_CurrentPage);
		}

		/// <summary>
		/// Goes back to a certain page
		/// </summary>
		/// <param name="backPage">Page to go back to</param>
		protected void SafeBack(IMainMenuPage backPage)
		{
			DeactivateCurrentPage();
			ActivateCurrentPage(backPage);
		}

		/// <summary>
		/// Goes back one page if possible
		/// </summary>
		public virtual void Back()
		{
			if (m_PageStack.Count == 0)
			{
				return;
			}

			DeactivateCurrentPage();
			m_PageStack.Pop();
			ActivateCurrentPage(m_PageStack.Pop());
		}

		/// <summary>
		/// Goes back to a specified page if possible
		/// </summary>
		/// <param name="backPage">Page to go back to</param>
		public virtual void Back(IMainMenuPage backPage)
		{
			Debug.Log($"[MainMenu] Back() called with backPage: {backPage}");
			Debug.Log($"[MainMenu] Current page: {m_CurrentPage}, Stack count: {m_PageStack.Count}");
			
			int count = m_PageStack.Count;
			if (count == 0)
			{
				Debug.Log("[MainMenu] Stack is empty, calling SafeBack()");
				SafeBack(backPage);
				return;
			}

			// Store pages temporarily to restore stack if not found
			List<IMainMenuPage> tempPages = new List<IMainMenuPage>();
			bool found = false;

			for (int i = count - 1; i >= 0; i--)
			{
				IMainMenuPage currentPage = m_PageStack.Pop();
				tempPages.Add(currentPage);
				if (currentPage == backPage)
				{
					Debug.Log("[MainMenu] Found backPage in stack, calling SafeBack()");
					found = true;
					SafeBack(backPage);
					return;
				}
			}

			// If not found, restore stack and use SafeBack
			if (!found)
			{
				Debug.Log("[MainMenu] backPage not found in stack, restoring stack and calling SafeBack()");
				// Restore stack in reverse order
				for (int i = tempPages.Count - 1; i >= 0; i--)
				{
					m_PageStack.Push(tempPages[i]);
				}
				SafeBack(backPage);
			}
		}
	}
}