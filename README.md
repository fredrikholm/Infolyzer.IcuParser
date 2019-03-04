# Infolyzer.IcuParser

An ICU message format parser for .NET Core 2.2 or later.

**Get it on NuGet:**

`PM> Install-Package Infolyzer.IcuParser`

## What problem does it solve?

Let's say you want to prevent unlimited login retries in your web app. After, let's say, three invalid login attempts, the account gets locked for five minutes. 

What I used to do in this scenario was simply to display a message like this:

- *Your account has been locked due to too many invalid login attempts.*

Simple as that. But then the user tries again, which displays this message at first:

- *Your account is still locked, please try again in 5 minutes.*

So, I need to add the capability to have variables in my messages, no problem. I just use something like this:

`Your account is still locked, please try again in {remainingMinutes} minutes.`

But some minutes later, as our impatient user hammers the keyboard, this I display this:

- *Your account is still locked, please try again in 1 minutes.*

Not so nice. 

Some people use the pragmatic but hacky approch to cover all variants:

- *Your account is still locked, please try again in X minute(s).*

Come on, sure we can do better than that, right? 

Yes we can. Enter ICU message formatting.


## Smarter Message Formatting 

The cool thing about this format is that it supports pluralization. With a slightly more complex message template I can display any of these three variations:

- ...please try again in 5 minutes
- ...please try again in about one minute
- ...please try again in just a moment

Here's the new message template:

```
Your account is still locked, please try again in {
    remainingMinutes, plural,
        =0 {just a moment}
        =1 {about one minute}
        other {in # minutes}
}
```

Here I have used some indentation to make it more readable, but typically these expressions live in files the don't work well with line breaks, like JSON files. So in my translation files, the above would look more like this:

```json
{ "StillLockedOut": "Your account is still locked, please try again in {remainingMinutes, plural, =0 {just a moment} =1 {about one minute} other {in # minutes}}" }
```
There is also support for set-based selectors, so I could utilize a gender variable to use the correct pronouns. Yeah! 

## Links

- More info about the ICU way of formatting messages can be fould here: 
  http://userguide.icu-project.org/formatparse/messages

- General info about ICU:
  http://site.icu-project.org/
