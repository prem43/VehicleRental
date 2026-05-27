// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
    const chatbot = document.getElementById('vrChatbot');
    if (!chatbot) return;

    const panel = chatbot.querySelector('.vr-chatbot-panel');
    const toggle = chatbot.querySelector('.vr-chatbot-toggle');
    const close = chatbot.querySelector('.vr-chatbot-close');
    const messages = chatbot.querySelector('.vr-chatbot-messages');
    const form = chatbot.querySelector('.vr-chatbot-input');
    const input = form.querySelector('input');

    const replies = {
        booking: 'For booking, open Vehicles, choose an available vehicle, select your dates, and submit a rental request. The seller will approve or reject it from their panel.',
        payment: 'Payment is a dummy flow in this project. After the seller approves your rental request, open My Rentals and click Dummy Pay.',
        seller: 'Seller accounts are created as Pending. Admin must approve the seller before they can log in and list vehicles.',
        documents: 'Sellers should upload clear vehicle images and registration or insurance documents while adding a vehicle. Admin reviews them before approval.',
        support: 'For urgent help, share your request ID, vehicle name, date range, and issue. A support/admin user can then check the rental request.'
    };

    function addMessage(text, type) {
        const bubble = document.createElement('div');
        bubble.className = `vr-message ${type}`;
        bubble.textContent = text;
        messages.appendChild(bubble);
        messages.scrollTop = messages.scrollHeight;
    }

    function answer(text) {
        const normalized = text.toLowerCase();
        let reply = replies.support;
        if (normalized.includes('book') || normalized.includes('rent') || normalized.includes('date')) reply = replies.booking;
        if (normalized.includes('pay') || normalized.includes('amount') || normalized.includes('transaction')) reply = replies.payment;
        if (normalized.includes('seller') || normalized.includes('approval') || normalized.includes('approve')) reply = replies.seller;
        if (normalized.includes('document') || normalized.includes('image') || normalized.includes('insurance')) reply = replies.documents;
        addMessage(reply, 'bot');
    }

    toggle.addEventListener('click', function () {
        chatbot.classList.add('open');
        panel.querySelector('input').focus();
    });

    close.addEventListener('click', function () {
        chatbot.classList.remove('open');
    });

    chatbot.querySelectorAll('.vr-chatbot-options button').forEach(button => {
        button.addEventListener('click', function () {
            addMessage(this.textContent, 'user');
            addMessage(replies[this.dataset.topic], 'bot');
        });
    });

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        const text = input.value.trim();
        if (!text) return;
        addMessage(text, 'user');
        input.value = '';
        answer(text);
    });
});
